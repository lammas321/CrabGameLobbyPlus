using System;
using System.Collections.Generic;

namespace LobbyPlus.Systems
{
    // Items are dequeued in a random order, can endlessly be dequeued, and the first and last items dequeued will never be the same (unless the length of items is 1), and will always return default if the length of items is 0
    public class RandomEndlessQueue<T>(IEnumerable<T> items)
    {
        private readonly Random random = new();
        private readonly T[] items = [.. items];
        private List<T> queue = [.. items];
        private T lastItem = default;

        public T Dequeue()
        {
            if (items.Length == 0)
                return default;

            bool isNewQueue = queue.Count == 0 && items.Length > 1;
            if (isNewQueue)
                queue = [.. items];

            int lastIndex = isNewQueue ? queue.IndexOf(lastItem) : queue.Count;
            int index = random.Next(isNewQueue ? queue.Count - 1 : queue.Count);
            if (index >= lastIndex)
                index++;

            lastItem = queue[index];
            queue.RemoveAt(index);
            return lastItem;
        }
    }
}