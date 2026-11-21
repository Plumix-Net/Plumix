namespace Plumix.UI.PathOps;

// C#-only infrastructure: port of skia/src/base/SkTSort.h at chrome/m119 (the Skia inside SkiaSharp 3.119), behind
// Path.Combine; SkDCubic::searchRoots and the later pathops layers sort with it. Line for line: Skia's names,
// operation order and float/double types are kept. Introsort is not stable, so the exact algorithm matters for the
// order of equal elements. Pointer ranges (begin, end) become spans; a List<T> is sorted through
// CollectionsMarshal.AsSpan.

internal static partial class SkPathOps
{
    public static void SkTHeapSortSiftUp<T>(Span<T> array, int root, int bottom, Func<T, T, bool> lessThan)
    {
        T x = array[root - 1];
        int start = root;
        int j = root << 1;
        while (j <= bottom)
        {
            if (j < bottom && lessThan(array[j - 1], array[j]))
            {
                ++j;
            }

            array[root - 1] = array[j - 1];
            root = j;
            j = root << 1;
        }

        j = root >> 1;
        while (j >= start)
        {
            if (lessThan(array[j - 1], x))
            {
                array[root - 1] = array[j - 1];
                root = j;
                j = root >> 1;
            }
            else
            {
                break;
            }
        }

        array[root - 1] = x;
    }

    public static void SkTHeapSortSiftDown<T>(Span<T> array, int root, int bottom, Func<T, T, bool> lessThan)
    {
        T x = array[root - 1];
        int child = root << 1;
        while (child <= bottom)
        {
            if (child < bottom && lessThan(array[child - 1], array[child]))
            {
                ++child;
            }

            if (lessThan(x, array[child - 1]))
            {
                array[root - 1] = array[child - 1];
                root = child;
                child = root << 1;
            }
            else
            {
                break;
            }
        }

        array[root - 1] = x;
    }

    public static void SkTHeapSort<T>(Span<T> array, int count, Func<T, T, bool> lessThan)
    {
        for (int i = count >> 1; i > 0; --i)
        {
            SkTHeapSortSiftDown(array, i, count, lessThan);
        }

        for (int i = count - 1; i > 0; --i)
        {
            (array[0], array[i]) = (array[i], array[0]);
            SkTHeapSortSiftUp(array, 1, i, lessThan);
        }
    }

    public static void SkTInsertionSort<T>(Span<T> left, int count, Func<T, T, bool> lessThan)
    {
        int right = count - 1;
        for (int next = 1; next <= right; ++next)
        {
            if (!lessThan(left[next], left[next - 1]))
            {
                continue;
            }

            T insert = left[next];
            int hole = next;
            do
            {
                left[hole] = left[hole - 1];
                --hole;
            } while (0 < hole && lessThan(insert, left[hole - 1]));

            left[hole] = insert;
        }
    }

    // Returns the pivot's index within left.
    public static int SkTQSortPartition<T>(Span<T> left, int count, int pivot, Func<T, T, bool> lessThan)
    {
        int right = count - 1;
        T pivotValue = left[pivot];
        (left[pivot], left[right]) = (left[right], left[pivot]);
        int newPivot = 0;
        int index = 0;
        while (index < right)
        {
            if (lessThan(left[index], pivotValue))
            {
                (left[index], left[newPivot]) = (left[newPivot], left[index]);
                newPivot += 1;
            }

            index += 1;
        }

        (left[newPivot], left[right]) = (left[right], left[newPivot]);
        return newPivot;
    }

    /*  Introsort is a modified Quicksort.
     *  When the region to be sorted is a small constant size, it uses Insertion Sort.
     *  When depth becomes zero, it switches over to Heap Sort.
     *  This implementation recurses on the left region after pivoting and loops on the right,
     *    we already limit the stack depth by switching to heap sort,
     *    and cache locality on the data appears more important than saving a few stack frames.
     */
    public static void SkTIntroSort<T>(int depth, Span<T> left, int count, Func<T, T, bool> lessThan)
    {
        for (;;)
        {
            if (count <= 32)
            {
                SkTInsertionSort(left, count, lessThan);
                return;
            }

            if (depth == 0)
            {
                SkTHeapSort(left, count, lessThan);
                return;
            }

            --depth;

            int middle = (count - 1) >> 1;
            int pivotCount = SkTQSortPartition(left, count, middle, lessThan);

            SkTIntroSort(depth, left, pivotCount, lessThan);
            left = left[(pivotCount + 1)..];
            count -= pivotCount + 1;
        }
    }

    /** Sorts the region using comparator lessThan using Introsort. */
    public static void SkTQSort<T>(Span<T> range, Func<T, T, bool> lessThan)
    {
        int n = range.Length;
        if (n <= 1)
        {
            return;
        }

        // Limit Introsort recursion depth to no more than 2 * ceil(log2(n-1)).
        int depth = 2 * SkNextLog2((uint)(n - 1));
        SkTIntroSort(depth, range, n, lessThan);
    }

    /** Sorts the region using comparator 'a < b' using Introsort. */
    public static void SkTQSort(Span<double> range)
    {
        SkTQSort(range, static (double a, double b) => a < b);
    }
}
