using ARMeilleure.Memory;
using Ryujinx.Cpu;
using Ryujinx.Graphics.Gpu;
using Ryujinx.HLE.HOS.Kernel.Process;
using Ryujinx.Memory;

namespace Ryujinx.HLE.HOS
{
    interface IArmProcessContext : IProcessContext
    {
        IDiskCacheLoadState Initialize(
            string titleIdText,
            string displayVersion,
            bool diskCacheEnabled,
            ulong codeAddress,
            ulong codeSize);
    }

    class ArmProcessContext<T> : IArmProcessContext where T : class, IVirtualMemoryManagerTracked, ICpuMemoryManager
    {
        private readonly ulong _pid;
        private readonly GpuContext _gpuContext;
        private readonly ICpuContext _cpuContext;
        private T _memoryManager;

        public ulong ReservedSize { get; }

        public IVirtualMemoryManager AddressSpace => _memoryManager;

        public ulong AddressSpaceSize { get; }

        public ArmProcessContext(
            ulong pid,
            ICpuEngine cpuEngine,
            GpuContext gpuContext,
            T memoryManager,
            ulong addressSpaceSize,
            bool for64Bit,
            ulong reservedSize = 0UL)
        {
            if (memoryManager is IRefCounted rc)
            {
                rc.IncrementReferenceCount();
            }

            gpuContext.RegisterProcess(pid, memoryManager);

            _pid = pid;
            _gpuContext = gpuContext;
            _cpuContext = cpuEngine.CreateCpuContext(memoryManager, for64Bit);
            _memoryManager = memoryManager;
            AddressSpaceSize = addressSpaceSize;
            ReservedSize = reservedSize;
        }

        public IExecutionContext CreateExecutionContext(ExceptionCallbacks exceptionCallbacks)
        {
            return _cpuContext.CreateExecutionContext(exceptionCallbacks);
        }

        public void Execute(IExecutionContext context, ulong codeAddress)
        {
            _cpuContext.Execute(context, codeAddress);
        }

        public IDiskCacheLoadState Initialize(
            string titleIdText,
            string displayVersion,
            bool diskCacheEnabled,
            ulong codeAddress,
            ulong codeSize)
        {
            _cpuContext.PrepareCodeRange(codeAddress, codeSize);
            return _cpuContext.LoadDiskCache(titleIdText, displayVersion, diskCacheEnabled);
        }

        public void InvalidateCacheRegion(ulong address, ulong size)
        {
            _cpuContext.InvalidateCacheRegion(address, size);
        }

        public void PatchCodeForNce(ulong textAddress, ulong textSize, ulong patchRegionAddress, ulong patchRegionSize)
        {
            _cpuContext.PatchCodeForNce(textAddress, textSize, patchRegionAddress, patchRegionSize);
        }

        public void Dispose()
        {
            if (_memoryManager is IRefCounted rc)
            {
                rc.DecrementReferenceCount();

                _memoryManager = null;
                _gpuContext.UnregisterProcess(_pid);
            }
        }
    }
}
