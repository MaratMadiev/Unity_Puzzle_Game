using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class SnapPoints
{
    Dictionary<SnapPointKey, SnapPoint> snapPoints;
    public Dictionary<SnapPointKey, SnapPoint> Dict { get => snapPoints; }

    public SnapPoints(Dictionary<SnapPointKey, SnapPoint> snapPoints)
    {
        this.snapPoints = snapPoints;
    }

    public SnapPoints()
    {
        snapPoints = new();
    }

    public void RecalculateFully(GameManager gm)
    {
        var roads = gm.Nodes.Values;

        snapPoints.Clear();

        foreach (var startGateway in gm.StartGateways)
        {
            var key = SnapPoints.SnapPointKey.GetKeyFromGateway(startGateway);
            if (!snapPoints.ContainsKey(key)) snapPoints[key] = new(key);
        }

        foreach (var endGateway in gm.EndGateways)
        {
            var key = SnapPoints.SnapPointKey.GetKeyFromGateway(endGateway);
            if (!snapPoints.ContainsKey(key)) snapPoints[key] = new(key);
        }


        foreach (var graphNode in roads)
        {
            var roadId = graphNode.Id;
            var road = gm.Nodes[roadId];
            AddRoadSection(road, true);
            AddRoadSection(road, false);
        }


    }

    public void AddRoadSection(GraphNode graphNode, bool addAsOutcoming)
    {
        var road = graphNode.Road;
        var key = SnapPointKey.GetKeyFromRoadSection(road, addAsOutcoming);
        if (!snapPoints.ContainsKey(key)) snapPoints[key] = new(key);

        if (addAsOutcoming)
        {
            snapPoints[key].AddOutcoming(road.Id);
        }
        else
        {
            snapPoints[key].AddIncoming(road.Id);
        }
    }


    public class SnapPoint
    {
        private List<int> incomingRoads;
        private List<int> outcomingRoads;
        private SnapPointKey key;

        public IReadOnlyList<int> IncomingRoads { get => incomingRoads; }
        public IReadOnlyList<int> OutcomingRoads { get => outcomingRoads; }

        public SnapPointKey Key { get => key; }

        public SnapPoint(SnapPointKey key)
        {
            incomingRoads = new List<int>();
            outcomingRoads = new List<int>();
            this.key = key;
        }

        public void AddOutcoming(int rs)
        {
            outcomingRoads.Add(rs);
        }

        public void AddIncoming(int rs)
        {
            incomingRoads.Add(rs);
        }
    }

    public struct SnapPointKey
    {
        public Vector2 xz;
        public int level;

        public SnapPointKey(Vector2 xz, int lvl)
        {
            this.xz = xz;
            this.level = lvl;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (GetType() != obj.GetType()) return false;
            var other = (SnapPointKey)obj;

            const int prec = 10;
            int x = (int)(xz.x * prec);
            int y = (int)(xz.y * prec);

            int x2 = (int)(other.xz.x * prec);
            int y2 = (int)(other.xz.y * prec);

            return other.level == level && x2 == x && y2 == y;
        }

        public override int GetHashCode()
        {
            const int prec = 10;
            int x = (int)(xz.x * prec);
            int y = (int)(xz.y * prec);
            return HashCode.Combine(level, x, y);
        }

        public static SnapPointKey GetKeyFromRoadSection(RoadSection rs, bool getFromStart)
        {
            int level = rs.Level;
            int levelEnd = level;
            if (rs.Type == RoadSection.RoadType.Upward) levelEnd++;
            if (rs.Type == RoadSection.RoadType.Downward) levelEnd--;

            SnapPointKey res;
            if (getFromStart)
            {
                res = new(rs.Curve.PointA, level);
            }
            else
            {
                res = new(rs.Curve.PointC, levelEnd);
            }

            return res;
        }

        public static SnapPointKey GetKeyFromGateway(Gateway gateway)
        {
            int level = 0;

            SnapPointKey res;
            if (gateway.Type== GatewayType.Finish)
            {
                res = new(gateway.Curve.PointA, level);
            }
            else
            {
                res = new(gateway.Curve.PointC, level);
            }

            return res;
        }
    }
}

