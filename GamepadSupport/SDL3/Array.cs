using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GamepadSupport.SDL3;

// Yes I know, but the stupid old dotnet version this game on doesn't know what `where T : unmanaged` means.
// I'll behave, I promise.
#pragma warning disable CS8500 // This takes the address of, gets the size of, or declares a pointer to a managed type

public sealed unsafe class SDLArray<T> : IEnumerable<T>, IDisposable {
    private readonly T* array;
    public readonly int Count;
    private bool isDisposed;

    public SDLArray(void* ptr, int count) {
        this.array = (T*)ptr;
        this.Count = count;
    }

    public T this[int index] {
        get {
            if (this.isDisposed) throw new ObjectDisposedException(this.GetType().FullName);
            else if (index < 0) throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be non-negative");
            else if (index >= this.Count) throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be in bounds");

            return this.array[index];
        }
    }

    public void Dispose() {
        if (isDisposed) return;
        this.isDisposed = true;
        SDL.Free(this.array);
    }

    public IEnumerator<T> GetEnumerator() => new Enumerator(this);
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => new Enumerator(this);

    public struct Enumerator : IEnumerator<T> {
        private readonly SDLArray<T> array;
        private int index;

        internal Enumerator(SDLArray<T> array) {
            this.array = array;
            this.index = -1;
        }

        public bool MoveNext() => ++this.index < this.array.Count;
        public void Reset() => this.index = -1;

        public readonly T Current => this.array[this.index];

        readonly object System.Collections.IEnumerator.Current => this.array[this.index];
        readonly void IDisposable.Dispose() { }
    }
}

// T* can't be used as a type parameter, so this has to be a separate class
public sealed unsafe class SDLPointerArray<T> : IEnumerable<T>, IDisposable {
    private readonly T** array;
    public readonly int Count;
    private bool isDisposed;

    public SDLPointerArray(void* array, int count) {
        this.array = (T**)array;
        this.Count = count;
    }

    public T this[int index] {
        get {
            if (this.isDisposed) throw new ObjectDisposedException(this.GetType().FullName);
            else if (index < 0) throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be non-negative");
            else if (index >= this.Count) throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be in bounds");
            Debug.Assert(this.array[index] != null);
            return *this.array[index];
        }
    }

    public void Dispose() {
        if (isDisposed) return;
        this.isDisposed = true;
        SDL.Free(array);
    }

    public IEnumerator<T> GetEnumerator() => new Enumerator(this);
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => new Enumerator(this);

    public struct Enumerator : IEnumerator<T> {
        private readonly SDLPointerArray<T> array;
        private int index;

        internal Enumerator(SDLPointerArray<T> array) {
            this.array = array;
            this.index = -1;
        }

        public bool MoveNext() => ++this.index < this.array.Count;
        public void Reset() => this.index = -1;

        public readonly T Current => this.array[this.index];

        readonly object System.Collections.IEnumerator.Current => this.array[this.index];
        readonly void IDisposable.Dispose() { }
    }
}
