namespace ProjectManager.Models;

/// <summary>
/// ComfyUI 内存管理模式枚举
/// </summary>
public enum MemoryManagementMode
{
    /// <summary>
    /// 不设置特定模式（默认）
    /// </summary>
    None,
    
    /// <summary>
    /// 仅使用GPU (--gpu-only)
    /// </summary>
    GpuOnly,
    
    /// <summary>
    /// 高显存模式 (--highvram)
    /// </summary>
    HighVram,
    
    /// <summary>
    /// 标准显存模式 (--normalvram)
    /// </summary>
    NormalVram,
    
    /// <summary>
    /// 低显存模式 (--lowvram)
    /// </summary>
    LowVram,
    
    /// <summary>
    /// 无显存模式 (--novram)
    /// </summary>
    NoVram,
    
    /// <summary>
    /// 使用CPU运行 (--cpu)
    /// </summary>
    CpuMode
}

/// <summary>
/// ComfyUI UNet模型精度枚举
/// </summary>
public enum UNetPrecisionMode
{
    /// <summary>
    /// 不设置特定精度（默认）
    /// </summary>
    None,
    
    /// <summary>
    /// FP32 UNet (--fp32-unet)
    /// </summary>
    FP32,
    
    /// <summary>
    /// FP64 UNet (--fp64-unet)
    /// </summary>
    FP64,
    
    /// <summary>
    /// BF16 UNet (--bf16-unet)
    /// </summary>
    BF16,
    
    /// <summary>
    /// FP16 UNet (--fp16-unet)
    /// </summary>
    FP16,
    
    /// <summary>
    /// FP8 E4M3FN UNet (--fp8_e4m3fn-unet)
    /// </summary>
    FP8_E4M3FN,
    
    /// <summary>
    /// FP8 E5M2 UNet (--fp8_e5m2-unet)
    /// </summary>
    FP8_E5M2,
    
    /// <summary>
    /// FP8 E8M0FNU UNet (--fp8_e8m0fnu-unet)
    /// </summary>
    FP8_E8M0FNU
}

/// <summary>
/// ComfyUI VAE模型精度枚举
/// </summary>
public enum VAEPrecisionMode
{
    /// <summary>
    /// 不设置特定精度（默认）
    /// </summary>
    None,
    
    /// <summary>
    /// FP16 VAE (--fp16-vae)
    /// </summary>
    FP16,
    
    /// <summary>
    /// FP32 VAE (--fp32-vae)
    /// </summary>
    FP32,
    
    /// <summary>
    /// BF16 VAE (--bf16-vae)
    /// </summary>
    BF16,
    
    /// <summary>
    /// CPU VAE (--cpu-vae)
    /// </summary>
    CPU
}

/// <summary>
/// ComfyUI 注意力算法枚举
/// </summary>
public enum AttentionAlgorithmMode
{
    /// <summary>
    /// 不设置特定算法（默认）
    /// </summary>
    None,
    
    /// <summary>
    /// 使用分割交叉注意力 (--use-split-cross-attention)
    /// </summary>
    SplitCrossAttention,
    
    /// <summary>
    /// 使用四元交叉注意力 (--use-quad-cross-attention)
    /// </summary>
    QuadCrossAttention,
    
    /// <summary>
    /// 使用PyTorch交叉注意力 (--use-pytorch-cross-attention)
    /// </summary>
    PytorchCrossAttention,
    
    /// <summary>
    /// 使用Sage注意力 (--use-sage-attention)
    /// </summary>
    SageAttention,
    
    /// <summary>
    /// 使用Flash注意力 (--use-flash-attention)
    /// </summary>
    FlashAttention
}

/// <summary>
/// ComfyUI 缓存模式枚举
/// </summary>
public enum CacheMode
{
    /// <summary>
    /// 默认缓存模式
    /// </summary>
    Default,
    
    /// <summary>
    /// 经典缓存 (--cache-classic)
    /// </summary>
    Classic,
    
    /// <summary>
    /// 无缓存 (--cache-none)
    /// </summary>
    None,
    
    /// <summary>
    /// LRU缓存 (--cache-lru)
    /// </summary>
    LRU
}
