using System.Drawing;

namespace OptAssignTest.Framework
{
    public interface IPathGenerator
    {
        /// <summary>
        /// Get position for the given frame.
        /// </summary>
        /// <returns><c>null</c> if position is not defined for the given frame.</returns>
        Point? GetPosition(uint frame); // TODO: think... nullable is not really good. or maybe 'null' means 'hidden'?

        /// <summary>
        /// Check if path is finished for the given frame.
        /// </summary>
        /// <returns><c>true</c> for completed path.</returns>
        bool IsDone(uint frame);

        /// <summary>
        /// Check if path is nearly finished, to allow exclusion of cases where the car is right at the edge of the frame
        /// </summary>
        /// <returns><c>true</c> for completed path.</returns>
        bool NearComplete(uint frame);
    }
}
