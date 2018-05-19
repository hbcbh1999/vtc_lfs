using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace OptAssignTest.Framework
{
    class SectionPath : IPathGenerator
    {
        #region Inner declarations

        // TODO: think - might be moved out for complex trajectories

        private class PathSection
        {
            private readonly uint _from;
            private readonly uint _to;
            private readonly Func<uint, Point> _pathGenerator;

            private readonly Path.Vector _offset; 

            public PathSection(uint from, uint to, Func<uint, Point> pathGenerator, Path.Vector? offset = null)
            {
                _from = from;
                _to = to;
                _pathGenerator = pathGenerator;
                _offset = offset.GetValueOrDefault(Path.Vector.Zero); // no offset by default
            }

            internal bool Contains(uint frame)
            {
                return (frame >= _from) && (frame <= _to); // TODO: think - should it be strict for both sides?
            }

            internal Point GetPoint(uint frame)
            {
                return _pathGenerator(frame) + _offset;
            }

            internal bool IsFinished(uint frame)
            {
                return frame > _to;
            }

            internal bool NearComplete(uint frame)
            {
                var percentage = (double)frame / _to;
                return (percentage > .98);
            }
        }

        #endregion

        private readonly List<PathSection> _sections = new List<PathSection>();

        public Point? GetPosition(uint frame)
        {
            foreach (var section in _sections.Where(s => s.Contains(frame))) // ER: can be optimized if list is sorted
            {
                return section.GetPoint(frame);
            }
            return null; // TODO: think... not really good
        }

        public bool IsDone(uint frame)
        {
            // check if all sections are finished
            return _sections.All(section => section.IsFinished(frame));
        }

        public bool NearComplete(uint frame)
        {
            // check if all sections are finished
            return _sections.All(section => section.NearComplete(frame));
        }

        public SectionPath AddSegment(uint from, uint to, Func<uint, Point> pathGenerator, Path.Vector? offset = null)
        {
            // validate inputs
            if (pathGenerator == null) throw new ArgumentNullException(nameof(pathGenerator));
            if (from >= to) throw new ArgumentException("Path definition is wrong");

            if (_sections.Any(pathSection => pathSection.Contains(from) || pathSection.Contains(to)))
                throw new ArgumentException("Segment intersects with another segment.");

            // register new path section
            var section = new PathSection(from, to, pathGenerator, offset);
            _sections.Add(section);

            return this;
        }
    }
}