/*
 * Blue Raja Package
 * Imported 04/04/2025
 * https://github.com/BlueRaja/High-Speed-Priority-Queue-for-C-Sharp/wiki/License
*/
namespace Priority_Queue
{
    public class StablePriorityQueueNode : FastPriorityQueueNode
    {
        /// <summary>
        /// Represents the order the node was inserted in
        /// </summary>
        public long InsertionIndex { get; internal set; }
    }
}
