namespace SampleProject
{
    internal class Program
    {
        static void Main(string[] args)
        {
            int[] sortedArray = { 1, 3, 5, 7, 9, 11, 13 };
            int target = 7;

            int result = BinarySearch(sortedArray, target);

            if (result != -1)
                Console.WriteLine($"Found {target} at {result}.");
            else
                Console.WriteLine($"Not Found {target}");

            Console.ReadLine();
        }

        static int BinarySearch(int[] arr, int target)
        {
            int left = 0;
            int right = arr.Length - 1;

            while (left <= right)
            {
                int mid = left + (right - left) / 2;


                if (arr[mid] == target)
                    return mid;

                if (arr[mid] < target)
                    left = mid + 1; // Tìm phía bên phải
                else
                    right = mid - 1; // Tìm phía bên trái
            }

            return -1; // Không tìm thấy
        }
    }
}
