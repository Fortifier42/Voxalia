//
// This file is part of the game Voxalia, created by FreneticXYZ.
// This code is Copyright (C) 2016 FreneticXYZ under the terms of the MIT license.
// See README.md or LICENSE.txt for contents of the MIT license.
// If these are not available, see https://opensource.org/licenses/MIT
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Voxalia.Shared;
using FreneticScript;
using Priority_Queue;

namespace Voxalia.ServerGame.WorldSystem
{
    public partial class Region
    {
        // Thanks to fullwall for the reference sources this was built off
        public List<Location> FindPath(Location startloc, Location endloc, double maxRadius, double goaldist, bool isAsync = false)
        {
            // TODO: Async safety!
            startloc = startloc.GetBlockLocation() + new Location(0.5, 0.5, 1.0);
            endloc = endloc.GetBlockLocation() + new Location(0.5, 0.5, 1.0);
            double mrsq = maxRadius * maxRadius;
            double gosq = goaldist * goaldist;
            if (startloc.DistanceSquared(endloc) > mrsq)
            {
                return null;
            }
            PathFindNode start = new PathFindNode() { Internal = startloc, F = 0, G = 0 };
            PathFindNode end = new PathFindNode() { Internal = endloc, F = 0, G = 0 };
            SimplePriorityQueue<PathFindNode> open = new SimplePriorityQueue<PathFindNode>();
            HashSet<Location> closed = new HashSet<Location>();
            HashSet<Location> openset = new HashSet<Location>();
            open.Enqueue(start, start.F);
            openset.Add(start.Internal);
            while (open.Count > 0)
            {
                PathFindNode next = open.Dequeue();
                openset.Remove(next.Internal);
                if (next.Internal.DistanceSquared(end.Internal) < gosq)
                {
                    return Reconstruct(next);
                }
                closed.Add(next.Internal);
                foreach (Location neighbor in PathFindNode.Neighbors)
                {
                    Location neighb = next.Internal + neighbor;
                    if (closed.Contains(neighb))
                    {
                        continue;
                    }
                    if (startloc.DistanceSquared(neighb) > mrsq)
                    {
                        continue;
                    }
                    // TODO: Check solidity from entities too!
                    if (GetBlockMaterial(neighb).GetSolidity() != MaterialSolidity.NONSOLID) // TODO: Better solidity check
                    {
                        continue;
                    }
                    if (GetBlockMaterial(neighb + new Location(0, 0, -1)).GetSolidity() == MaterialSolidity.NONSOLID
                        && GetBlockMaterial(neighb + new Location(0, 0, -2)).GetSolidity() == MaterialSolidity.NONSOLID
                        && GetBlockMaterial(next.Internal + new Location(0, 0, -1)).GetSolidity() == MaterialSolidity.NONSOLID
                        && GetBlockMaterial(next.Internal + new Location(0, 0, -2)).GetSolidity() == MaterialSolidity.NONSOLID)
                    {
                        continue;
                    }
                    PathFindNode node = new PathFindNode() { Internal = neighb };
                    node.G = next.G + 1; // Note: Distance beween 'node' and 'next' is 1.
                    node.F = node.G + node.Distance(end);
                    node.Parent = next;
                    if (openset.Contains(node.Internal))
                    {
                        continue;
                    }
                    open.Enqueue(node, node.F);
                    openset.Add(node.Internal);
                }
            }
            return null;
        }

        List<Location> Reconstruct(PathFindNode node)
        {
            List<Location> locs = new List<Location>();
            while (node != null)
            {
                locs.Add(node.Internal);
                node = node.Parent;
            }
            locs.Reverse();
            return locs;
        }
    }

    public class PathFindNode: FastPriorityQueueNode, IComparable<PathFindNode>, IEquatable<PathFindNode>, IComparer<PathFindNode>, IEqualityComparer<PathFindNode>
    {
        public Location Internal;
        
        public double F;

        public double G;

        public PathFindNode Parent;

        public static Location[] Neighbors = new Location[] { Location.UnitX, Location.UnitY, Location.UnitZ, -Location.UnitX, -Location.UnitY, -Location.UnitZ };

        /// <summary>
        /// Goes through a square root, yikes!
        /// </summary>
        public double Distance(PathFindNode other)
        {
            return Internal.Distance(other.Internal);
        }

        public int CompareTo(PathFindNode other)
        {
            if (other.F > F)
            {
                return 1;
            }
            else if (F > other.F)
            {
                return -1;
            }
            return 0;
        }

        public bool Equals(PathFindNode other)
        {
            if (other == null)
            {
                return false;
            }
            return other.Internal == this.Internal;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is PathFindNode))
            {
                return false;
            }
            return Equals((PathFindNode)obj);
        }

        public static bool operator ==(PathFindNode self, PathFindNode other)
        {
            if (ReferenceEquals(self, null) && ReferenceEquals(other, null))
            {
                return true;
            }
            if (ReferenceEquals(self, null) || ReferenceEquals(other, null))
            {
                return false;
            }
            return self.Equals(other);
        }

        public static bool operator !=(PathFindNode self, PathFindNode other)
        {
            if (ReferenceEquals(self, null) && ReferenceEquals(other, null))
            {
                return false;
            }
            if (ReferenceEquals(self, null) || ReferenceEquals(other, null))
            {
                return true;
            }
            return !self.Equals(other);
        }

        public override int GetHashCode()
        {
            return Internal.GetHashCode();
        }

        public int Compare(PathFindNode x, PathFindNode y)
        {
            return x.CompareTo(y);
        }

        public bool Equals(PathFindNode x, PathFindNode y)
        {
            return x == y;
        }

        public int GetHashCode(PathFindNode obj)
        {
            return obj.GetHashCode();
        }
    }
}
