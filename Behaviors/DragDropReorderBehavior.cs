using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Documents;

namespace ProjectManager.Behaviors
{
    /// <summary>
    /// 提供 ItemsControl 内部的拖拽重排功能，并支持指定句柄元素才能开始拖拽。
    /// 用法：
    /// - 在 ItemsControl 上设置 DragDropReorder.Enable="True"
    /// - 在模板中的句柄元素上设置 DragDropReorder.IsDragHandle="True"
    /// </summary>
    public static class DragDropReorder
    {
        #region Enable
        public static readonly DependencyProperty EnableProperty =
            DependencyProperty.RegisterAttached(
                "Enable",
                typeof(bool),
                typeof(DragDropReorder),
                new PropertyMetadata(false, OnEnableChanged));

        public static void SetEnable(DependencyObject element, bool value) => element.SetValue(EnableProperty, value);
        public static bool GetEnable(DependencyObject element) => (bool)element.GetValue(EnableProperty);
        #endregion

        #region IsDragHandle
        public static readonly DependencyProperty IsDragHandleProperty =
            DependencyProperty.RegisterAttached(
                "IsDragHandle",
                typeof(bool),
                typeof(DragDropReorder),
                new PropertyMetadata(false, OnIsDragHandleChanged));

        public static void SetIsDragHandle(DependencyObject element, bool value) => element.SetValue(IsDragHandleProperty, value);
        public static bool GetIsDragHandle(DependencyObject element) => (bool)element.GetValue(IsDragHandleProperty);
        #endregion

        private class State
        {
            public Point StartPoint;
            public object? DraggedItem;
            public bool IsMouseDown;
            public DragVisualAdorner? Adorner;
            public AdornerLayer? AdornerLayer;
        }

        private static readonly DependencyProperty StateProperty =
            DependencyProperty.RegisterAttached(
                "__State",
                typeof(State),
                typeof(DragDropReorder),
                new PropertyMetadata(null));

        private static State GetState(DependencyObject d)
        {
            var state = (State?)d.GetValue(StateProperty);
            if (state == null)
            {
                state = new State();
                d.SetValue(StateProperty, state);
            }
            return state;
        }

        private static void OnEnableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not ItemsControl itemsControl)
                return;

            if ((bool)e.NewValue)
            {
                itemsControl.AllowDrop = true;
                itemsControl.PreviewMouseLeftButtonDown += ItemsControl_PreviewMouseLeftButtonDown;
                itemsControl.PreviewMouseMove += ItemsControl_PreviewMouseMove;
                itemsControl.DragOver += ItemsControl_DragOver;
                itemsControl.Drop += ItemsControl_Drop;
                itemsControl.GiveFeedback += ItemsControl_GiveFeedback;
            }
            else
            {
                itemsControl.PreviewMouseLeftButtonDown -= ItemsControl_PreviewMouseLeftButtonDown;
                itemsControl.PreviewMouseMove -= ItemsControl_PreviewMouseMove;
                itemsControl.DragOver -= ItemsControl_DragOver;
                itemsControl.Drop -= ItemsControl_Drop;
                itemsControl.GiveFeedback -= ItemsControl_GiveFeedback;
            }
        }

        private static void ItemsControl_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not ItemsControl itemsControl) return;
            var state = GetState(itemsControl);

            // 仅当从句柄开始时才允许拖拽
            if (!IsFromHandle(e.OriginalSource as DependencyObject))
                return;

            state.StartPoint = e.GetPosition(itemsControl);
            state.IsMouseDown = true;

            var container = GetContainerFromElement(itemsControl, e.OriginalSource as DependencyObject);
            state.DraggedItem = container != null
                ? itemsControl.ItemContainerGenerator.ItemFromContainer(container)
                : null;
        }

        private static void ItemsControl_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not ItemsControl itemsControl) return;
            var state = GetState(itemsControl);

            if (!state.IsMouseDown || e.LeftButton != MouseButtonState.Pressed || state.DraggedItem == null)
                return;

            var currentPos = e.GetPosition(itemsControl);
            if (Math.Abs(currentPos.X - state.StartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(currentPos.Y - state.StartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                // 开始拖拽
                EnsureAdorner(itemsControl, state);
                var data = new DataObject(typeof(object), state.DraggedItem);
                DragDrop.DoDragDrop(itemsControl, data, DragDropEffects.Move);

                // 重置状态
                RemoveAdorner(itemsControl, state);
                state.IsMouseDown = false;
                state.DraggedItem = null;
            }
        }

        private static void ItemsControl_DragOver(object sender, DragEventArgs e)
        {
            if (sender is not ItemsControl itemsControl) return;
            // 仅同一列表 Move
            if (!e.Data.GetDataPresent(typeof(object)))
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }
            e.Effects = DragDropEffects.Move;
            e.Handled = true;

            // 更新预览位置
            var state = GetState(itemsControl);
            if (state.Adorner != null)
            {
                var p = e.GetPosition(itemsControl);
                state.Adorner.LeftOffset = p.X + 4; // 略微偏移避免挡住指针
                state.Adorner.TopOffset = p.Y + 4;
            }
        }

        private static void ItemsControl_Drop(object sender, DragEventArgs e)
        {
            if (sender is not ItemsControl itemsControl) return;
            if (!e.Data.GetDataPresent(typeof(object))) return;

            var draggedItem = e.Data.GetData(typeof(object));
            if (draggedItem == null) return;

            var target = GetItemUnderMouse(itemsControl, e.GetPosition(itemsControl));

            // 取源集合（优先从 ICollectionView.SourceCollection 获取）
            IList? list = null;
            if (itemsControl.ItemsSource is ICollectionView view)
                list = view.SourceCollection as IList;
            list ??= itemsControl.ItemsSource as IList;

            if (list == null) return; // 不支持的集合类型

            var oldIndex = list.IndexOf(draggedItem);
            if (oldIndex < 0) return;

            int newIndex;
            if (target == null || ReferenceEquals(target, draggedItem))
            {
                // 放到末尾
                newIndex = list.Count - 1;
            }
            else
            {
                newIndex = list.IndexOf(target);
            }

            if (newIndex < 0 || newIndex == oldIndex) return;

            Move(list, oldIndex, newIndex);

            // 如果使用视图，刷新以反映顺序变化
            if (itemsControl.ItemsSource is ICollectionView view2)
            {
                view2.Refresh();
            }

            // 尝试持久化新顺序（如果集合元素为 Project）
            try
            {
                // 获取 IProjectService（使用全局 App 服务容器）
                var svc = App.Services.GetService(typeof(ProjectManager.Services.IProjectService)) as ProjectManager.Services.IProjectService;
                if (svc != null)
                {
                    // 构建当前顺序的项目列表
                    if (itemsControl.ItemsSource is IEnumerable enumerable)
                    {
                        var ordered = new System.Collections.Generic.List<ProjectManager.Models.Project>();
                        foreach (var it in enumerable)
                        {
                            if (it is ProjectManager.Models.Project p)
                                ordered.Add(p);
                        }

                        // Fire-and-forget 保存顺序
                        _ = svc.SaveProjectsOrderAsync(ordered);
                    }
                }
            }
            catch { /* 忽略持久化错误，避免影响 UI */ }

            // 移除预览
            var state = GetState(itemsControl);
            RemoveAdorner(itemsControl, state);

            e.Handled = true;
        }

        private static void ItemsControl_GiveFeedback(object sender, GiveFeedbackEventArgs e)
        {
            // 使用自定义预览，不使用系统默认光标，以免冲突
            e.UseDefaultCursors = false;
            Mouse.SetCursor(Cursors.Arrow);
            e.Handled = true;
        }

        private static void Move(IList list, int oldIndex, int newIndex)
        {
            if (oldIndex == newIndex) return;

            // 尝试通过反射调用 Move 方法，以支持各种泛型类型的 ObservableCollection<T>
            // 因为 ObservableCollection<T> 并不继承自一个带有 Move 方法的非泛型基类
            var moveMethod = list.GetType().GetMethod("Move", new[] { typeof(int), typeof(int) });
            if (moveMethod != null)
            {
                moveMethod.Invoke(list, new object[] { oldIndex, newIndex });
                return;
            }

            // 检查集合是否只读，避免抛出异常
            if (list.IsReadOnly)
            {
                System.Diagnostics.Debug.WriteLine("DragDropReorder: Collection is read-only, cannot perform reorder.");
                return;
            }

            var item = list[oldIndex];
            list.RemoveAt(oldIndex);
            if (newIndex >= list.Count)
                list.Add(item);
            else
                list.Insert(newIndex, item);
        }

        private static object? GetItemUnderMouse(ItemsControl itemsControl, Point position)
        {
            var element = itemsControl.InputHitTest(position) as DependencyObject;
            if (element == null) return null;

            var container = GetContainerFromElement(itemsControl, element);
            if (container == null) return null;

            return itemsControl.ItemContainerGenerator.ItemFromContainer(container);
        }

        private static DependencyObject? GetContainerFromElement(ItemsControl itemsControl, DependencyObject? element)
        {
            if (element == null) return null;
            // 使用框架提供的方法更稳妥
            var container = ItemsControl.ContainerFromElement(itemsControl, element);
            if (container != null) return container;
            // 退回到视觉树向上查找 ContentPresenter
            return FindAncestor<ContentPresenter>(element);
        }

        private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T typed) return typed;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private static bool IsFromHandle(DependencyObject? source)
        {
            while (source != null)
            {
                if (source.ReadLocalValue(IsDragHandleProperty) is bool b && b)
                    return true;
                source = VisualTreeHelper.GetParent(source);
            }
            return false;
        }

        private static void OnIsDragHandleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not UIElement handle) return;

            if ((bool)e.NewValue)
            {
                handle.PreviewMouseLeftButtonDown += Handle_PreviewMouseLeftButtonDown;
                handle.PreviewMouseMove += Handle_PreviewMouseMove;
            }
            else
            {
                handle.PreviewMouseLeftButtonDown -= Handle_PreviewMouseLeftButtonDown;
                handle.PreviewMouseMove -= Handle_PreviewMouseMove;
            }
        }

        private static void Handle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not DependencyObject handle) return;
            var itemsControl = FindAncestor<ItemsControl>(handle);
            if (itemsControl == null) return;

            var state = GetState(itemsControl);
            state.StartPoint = e.GetPosition(itemsControl);
            state.IsMouseDown = true;

            var container = GetContainerFromElement(itemsControl, handle);
            state.DraggedItem = container != null
                ? itemsControl.ItemContainerGenerator.ItemFromContainer(container)
                : null;
        }

        private static void Handle_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not DependencyObject handle) return;
            var itemsControl = FindAncestor<ItemsControl>(handle);
            if (itemsControl == null) return;

            var state = GetState(itemsControl);
            if (!state.IsMouseDown || e.LeftButton != MouseButtonState.Pressed || state.DraggedItem == null)
                return;

            var currentPos = e.GetPosition(itemsControl);
            if (Math.Abs(currentPos.X - state.StartPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(currentPos.Y - state.StartPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                EnsureAdorner(itemsControl, state, handle);
                var data = new DataObject(typeof(object), state.DraggedItem);
                DragDrop.DoDragDrop(itemsControl, data, DragDropEffects.Move);

                RemoveAdorner(itemsControl, state);
                state.IsMouseDown = false;
                state.DraggedItem = null;
            }
        }

        private static void EnsureAdorner(ItemsControl itemsControl, State state, DependencyObject? originElement = null)
        {
            if (state.Adorner != null) return;

            var container = originElement != null
                ? GetContainerFromElement(itemsControl, originElement)
                : (itemsControl.ItemContainerGenerator.ContainerFromItem(state.DraggedItem) as DependencyObject);

            if (container == null) return;

            var layer = AdornerLayer.GetAdornerLayer(itemsControl);
            if (layer == null) return;

            var adorner = new DragVisualAdorner(itemsControl, container);
            state.Adorner = adorner;
            state.AdornerLayer = layer;
            layer.Add(adorner);

            var p = Mouse.GetPosition(itemsControl);
            adorner.LeftOffset = p.X + 4;
            adorner.TopOffset = p.Y + 4;
        }

        private static void RemoveAdorner(ItemsControl itemsControl, State state)
        {
            if (state.AdornerLayer != null && state.Adorner != null)
            {
                state.AdornerLayer.Remove(state.Adorner);
            }
            state.Adorner = null;
            state.AdornerLayer = null;
        }

        private class DragVisualAdorner : Adorner
        {
            private readonly VisualBrush _brush;
            private readonly Size _size;
            private double _left;
            private double _top;

            public double LeftOffset { get => _left; set { _left = value; InvalidateVisual(); } }
            public double TopOffset { get => _top; set { _top = value; InvalidateVisual(); } }

            public DragVisualAdorner(UIElement adornedElement, DependencyObject visualToShow)
                : base(adornedElement)
            {
                IsHitTestVisible = false;
                var element = visualToShow as FrameworkElement;
                var size = element?.RenderSize ?? new Size(100, 100);
                _size = new Size(Math.Max(1, size.Width), Math.Max(1, size.Height));

                _brush = new VisualBrush(visualToShow as Visual)
                {
                    Opacity = 0.75,
                    Stretch = Stretch.None,
                    AlignmentX = AlignmentX.Left,
                    AlignmentY = AlignmentY.Top
                };
            }

            protected override void OnRender(DrawingContext drawingContext)
            {
                base.OnRender(drawingContext);
                var rect = new Rect(new Point(LeftOffset, TopOffset), _size);
                drawingContext.DrawRectangle(_brush, null, rect);
            }
        }
    }
}
