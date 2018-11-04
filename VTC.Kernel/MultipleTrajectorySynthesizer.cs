﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTC.Common;
using VTC.Common.RegionConfig;
using VTC.Reporting;

namespace VTC.Kernel
{
    public class MultipleTrajectorySynthesizer
    {

        public List<Movement> TrajectoryPrototypes = new List<Movement>();

        public void GenerateSyntheticTrajectories(RegionConfig regionConfig, string filepath)
        {
            TrajectoryPrototypes.Clear();

            var approach1 = regionConfig.Regions.FirstOrDefault(kvp => kvp.Key.Contains("Approach 1")).Value;
            var approach2 = regionConfig.Regions.FirstOrDefault(kvp => kvp.Key.Contains("Approach 2")).Value;
            var approach3 = regionConfig.Regions.FirstOrDefault(kvp => kvp.Key.Contains("Approach 3")).Value;
            var approach4 = regionConfig.Regions.FirstOrDefault(kvp => kvp.Key.Contains("Approach 4")).Value;

            var exit1 = regionConfig.Regions.FirstOrDefault(kvp => kvp.Key.Contains("Exit 1")).Value;
            var exit2 = regionConfig.Regions.FirstOrDefault(kvp => kvp.Key.Contains("Exit 2")).Value;
            var exit3 = regionConfig.Regions.FirstOrDefault(kvp => kvp.Key.Contains("Exit 3")).Value;
            var exit4 = regionConfig.Regions.FirstOrDefault(kvp => kvp.Key.Contains("Exit 4")).Value;

            var sidewalk1 = regionConfig.Regions.FirstOrDefault(kvp => kvp.Key.Contains("Sidewalk 1")).Value;
            var sidewalk2 = regionConfig.Regions.FirstOrDefault(kvp => kvp.Key.Contains("Sidewalk 2")).Value;
            var sidewalk3 = regionConfig.Regions.FirstOrDefault(kvp => kvp.Key.Contains("Sidewalk 3")).Value;
            var sidewalk4 = regionConfig.Regions.FirstOrDefault(kvp => kvp.Key.Contains("Sidewalk 4")).Value;

            var road1Line = new RoadLine();
            if (approach1.Count > 0 && exit1.Count > 0)
            {
                road1Line.ApproachCentroidX = approach1.Centroid.X;
                road1Line.ApproachCentroidY = approach1.Centroid.Y;
                road1Line.ExitCentroidX = exit1.Centroid.X;
                road1Line.ExitCentroidY = exit1.Centroid.Y;
            }
            else
            {
                road1Line = null;
            }

            var road2Line = new RoadLine();
            if (approach2.Count > 0 && exit2.Count > 0)
            {
                road2Line.ApproachCentroidX = approach2.Centroid.X;
                road2Line.ApproachCentroidY = approach2.Centroid.Y;
                road2Line.ExitCentroidX = exit2.Centroid.X;
                road2Line.ExitCentroidY = exit2.Centroid.Y;
            }
            else
            {
                road2Line = null;
            }

            var road3Line = new RoadLine();
            if (approach3.Count > 0 && exit3.Count > 0)
            {
                road3Line.ApproachCentroidX = approach3.Centroid.X;
                road3Line.ApproachCentroidY = approach3.Centroid.Y;
                road3Line.ExitCentroidX = exit3.Centroid.X;
                road3Line.ExitCentroidY = exit3.Centroid.Y;
            }
            else
            {
                road3Line = null;
            }

            var road4Line = new RoadLine();
            if (approach4.Count > 0 && exit4.Count > 0)
            {
                road4Line.ApproachCentroidX = approach4.Centroid.X;
                road4Line.ApproachCentroidY = approach4.Centroid.Y;
                road4Line.ExitCentroidX = exit4.Centroid.X;
                road4Line.ExitCentroidY = exit4.Centroid.Y;
            }
            else
            {
                road4Line = null;
            }

            var crossing12 = new RoadLine();
            var crossing21 = new RoadLine();
            if (sidewalk1.Count > 0 && sidewalk2.Count > 0)
            {
                crossing12.ApproachCentroidX = sidewalk1.Centroid.X;
                crossing12.ApproachCentroidY = sidewalk1.Centroid.Y;
                crossing12.ExitCentroidX = sidewalk2.Centroid.X;
                crossing12.ExitCentroidY = sidewalk2.Centroid.Y;

                crossing21.ApproachCentroidX = sidewalk2.Centroid.X;
                crossing21.ApproachCentroidY = sidewalk2.Centroid.Y;
                crossing21.ExitCentroidX = sidewalk1.Centroid.X;
                crossing21.ExitCentroidY = sidewalk1.Centroid.Y;
            }
            else
            {
                crossing12 = null;
                crossing21 = null;
            }

            var crossing23 = new RoadLine();
            var crossing32 = new RoadLine();
            if (sidewalk2.Count > 0 && sidewalk3.Count > 0)
            {
                crossing23.ApproachCentroidX = sidewalk2.Centroid.X;
                crossing23.ApproachCentroidY = sidewalk2.Centroid.Y;
                crossing23.ExitCentroidX = sidewalk3.Centroid.X;
                crossing23.ExitCentroidY = sidewalk3.Centroid.Y;

                crossing32.ApproachCentroidX = sidewalk3.Centroid.X;
                crossing32.ApproachCentroidY = sidewalk3.Centroid.Y;
                crossing32.ExitCentroidX = sidewalk2.Centroid.X;
                crossing32.ExitCentroidY = sidewalk2.Centroid.Y;
            }
            else
            {
                crossing23 = null;
                crossing32 = null;
            }

            var crossing34 = new RoadLine();
            var crossing43 = new RoadLine();
            if (sidewalk3.Count > 0 && sidewalk4.Count > 0)
            {
                crossing34.ApproachCentroidX = sidewalk3.Centroid.X;
                crossing34.ApproachCentroidY = sidewalk3.Centroid.Y;
                crossing34.ExitCentroidX = sidewalk4.Centroid.X;
                crossing34.ExitCentroidY = sidewalk4.Centroid.Y;

                crossing43.ApproachCentroidX = sidewalk4.Centroid.X;
                crossing43.ApproachCentroidY = sidewalk4.Centroid.Y;
                crossing43.ExitCentroidX = sidewalk3.Centroid.X;
                crossing43.ExitCentroidY = sidewalk3.Centroid.Y;
            }
            else
            {
                crossing34 = null;
                crossing43 = null;
            }

            var crossing41 = new RoadLine();
            var crossing14 = new RoadLine();
            if (sidewalk4.Count > 0 && sidewalk1.Count > 0)
            {
                crossing41.ApproachCentroidX = sidewalk4.Centroid.X;
                crossing41.ApproachCentroidY = sidewalk4.Centroid.Y;
                crossing41.ExitCentroidX = sidewalk1.Centroid.X;
                crossing41.ExitCentroidY = sidewalk1.Centroid.Y;

                crossing14.ApproachCentroidX = sidewalk1.Centroid.X;
                crossing14.ApproachCentroidY = sidewalk1.Centroid.Y;
                crossing14.ExitCentroidX = sidewalk4.Centroid.X;
                crossing14.ExitCentroidY = sidewalk4.Centroid.Y;
            }
            else
            {
                crossing14 = null;
                crossing41 = null;
            }

            if (road1Line != null && road2Line != null)
            {
                var road1Left = PolygonTrajectorySynthesizer.SyntheticTrajectory(approach1, exit2, road1Line, road2Line);
                var movementRoad1Left = new Movement("Approach 1", "Exit 2", Turn.Left, ObjectType.Car, road1Left.StateHistory,0);
                TrajectoryPrototypes.Add(movementRoad1Left);
                var t2 = new TrajectoryLogger(movementRoad1Left);
                t2.Save(filepath);

                var road2Right = PolygonTrajectorySynthesizer.SyntheticTrajectory(approach2, exit1, road2Line, road1Line);
                var movementRoad2Right = new Movement("Approach 2", "Exit 1", Turn.Right, ObjectType.Car,
                    road2Right.StateHistory,0);
                TrajectoryPrototypes.Add(movementRoad2Right);
                var t6 = new TrajectoryLogger(movementRoad2Right);
                t6.Save(filepath);
            }

            if (road1Line != null && road4Line != null)
            {
                var road1Right = PolygonTrajectorySynthesizer.SyntheticTrajectory(approach1, exit4, road1Line, road4Line);
                var movementRoad1Right = new Movement("Approach 1", "Exit 4", Turn.Right, ObjectType.Car, road1Right.StateHistory,0);
                TrajectoryPrototypes.Add(movementRoad1Right);
                var t3 = new TrajectoryLogger(movementRoad1Right);
                t3.Save(filepath);

                var road4Left = PolygonTrajectorySynthesizer.SyntheticTrajectory(approach4, exit1, road4Line, road1Line);  
                var movementRoad4Left = new Movement("Approach 4", "Exit 1", Turn.Left, ObjectType.Car, road4Left.StateHistory,0);
                TrajectoryPrototypes.Add(movementRoad4Left);
                var t11 = new TrajectoryLogger(movementRoad4Left);
                t11.Save(filepath);
            }

            if (road1Line != null)
            {
                var road1Straight = PolygonTrajectorySynthesizer.SyntheticTrajectory(approach1, exit1, road1Line);
                var movementRoad2Straight = new Movement("Approach 1", "Exit 1", Turn.Straight, ObjectType.Car, road1Straight.StateHistory,0);
                TrajectoryPrototypes.Add(movementRoad2Straight);
                var tl = new TrajectoryLogger(movementRoad2Straight);
                tl.Save(filepath);
            }

            if (road2Line != null && road3Line != null)
            {
                var road2Left = PolygonTrajectorySynthesizer.SyntheticTrajectory(approach2, exit3, road2Line, road3Line);
                var movementRoad2Left = new Movement("Approach 2", "Exit 3", Turn.Left, ObjectType.Car, road2Left.StateHistory,0);
                TrajectoryPrototypes.Add(movementRoad2Left);
                var t5 = new TrajectoryLogger(movementRoad2Left);
                t5.Save(filepath);

                var road3Right = PolygonTrajectorySynthesizer.SyntheticTrajectory(approach3, exit2, road3Line, road2Line);
                var movementRoad3Right = new Movement("Approach 3", "Exit 2", Turn.Right, ObjectType.Car, road3Right.StateHistory,0);
                TrajectoryPrototypes.Add(movementRoad3Right);
                var t9 = new TrajectoryLogger(movementRoad3Right);
                t9.Save(filepath);
            }

            if (road2Line != null)
            {
                var road2Straight = PolygonTrajectorySynthesizer.SyntheticTrajectory(approach2, exit2, road2Line);
                var movementRoad2Straight = new Movement("Approach 2", "Exit 2", Turn.Straight, ObjectType.Car, road2Straight.StateHistory,0);
                TrajectoryPrototypes.Add(movementRoad2Straight);
                var t4 = new TrajectoryLogger(movementRoad2Straight);
                t4.Save(filepath);
            }

            if (road3Line != null && road4Line != null)
            {
                var road3Left = PolygonTrajectorySynthesizer.SyntheticTrajectory(approach3, exit4, road3Line, road4Line);
                var movementRoad3Left = new Movement("Approach 3", "Exit 4", Turn.Left, ObjectType.Car, road3Left.StateHistory,0);
                TrajectoryPrototypes.Add(movementRoad3Left);
                var t8 = new TrajectoryLogger(movementRoad3Left);
                t8.Save(filepath);

                var road4Right = PolygonTrajectorySynthesizer.SyntheticTrajectory(approach4, exit3, road4Line, road3Line);
                var movementRoad4Right = new Movement("Approach 4", "Exit 3", Turn.Right, ObjectType.Car, road4Right.StateHistory,0);
                TrajectoryPrototypes.Add(movementRoad4Right);
                var t12 = new TrajectoryLogger(movementRoad4Right);
                t12.Save(filepath);
            }

            if (road3Line != null)
            {
                var road3Straight = PolygonTrajectorySynthesizer.SyntheticTrajectory(approach3, exit3, road3Line);
                var movementRoad3Straight = new Movement("Approach 3", "Exit 3", Turn.Straight, ObjectType.Car, road3Straight.StateHistory,0);
                TrajectoryPrototypes.Add(movementRoad3Straight);
                var t7 = new TrajectoryLogger(movementRoad3Straight);
                t7.Save(filepath);
            }

            if (road4Line != null)
            {
                var road4Straight = PolygonTrajectorySynthesizer.SyntheticTrajectory(approach4, exit4, road4Line);
                var movementRoad4Straight = new Movement("Approach 4", "Exit 4", Turn.Straight, ObjectType.Car, road4Straight.StateHistory,0);
                TrajectoryPrototypes.Add(movementRoad4Straight);
                var t10 = new TrajectoryLogger(movementRoad4Straight);
                t10.Save(filepath);
            }

            if ((crossing12 != null) && (crossing21 != null))
            {
                var crossing12Walk = PolygonTrajectorySynthesizer.SyntheticTrajectory(sidewalk1, sidewalk2, crossing12);
                var movementCrossing12Walk = new Movement("Sidewalk 1", "Sidewalk 2", Turn.Crossing, ObjectType.Person, crossing12Walk.StateHistory,0);
                TrajectoryPrototypes.Add(movementCrossing12Walk);
                var t11 = new TrajectoryLogger(movementCrossing12Walk);
                t11.Save(filepath);

                var crossing21Walk = PolygonTrajectorySynthesizer.SyntheticTrajectory(sidewalk2, sidewalk1, crossing21);
                var movementCrossing21Walk = new Movement("Sidewalk 2", "Sidewalk 1", Turn.Crossing, ObjectType.Person, crossing21Walk.StateHistory,0);
                TrajectoryPrototypes.Add(movementCrossing21Walk);
                var t12 = new TrajectoryLogger(movementCrossing21Walk);
                t12.Save(filepath);
            }

            if ((crossing23 != null) && (crossing32 != null))
            {
                var crossing23Walk = PolygonTrajectorySynthesizer.SyntheticTrajectory(sidewalk2, sidewalk3, crossing23);
                var movementCrossing23Walk = new Movement("Sidewalk 2", "Sidewalk 3", Turn.Crossing, ObjectType.Person, crossing23Walk.StateHistory,0);
                TrajectoryPrototypes.Add(movementCrossing23Walk);
                var t11 = new TrajectoryLogger(movementCrossing23Walk);
                t11.Save(filepath);

                var crossing32Walk = PolygonTrajectorySynthesizer.SyntheticTrajectory(sidewalk3, sidewalk2, crossing32);
                var movementCrossing32Walk = new Movement("Sidewalk 3", "Sidewalk 2", Turn.Crossing, ObjectType.Person, crossing32Walk.StateHistory,0);
                TrajectoryPrototypes.Add(movementCrossing32Walk);
                var t12 = new TrajectoryLogger(movementCrossing32Walk);
                t12.Save(filepath);
            }

            if ((crossing34 != null) && (crossing43 != null))
            {
                var crossing34Walk = PolygonTrajectorySynthesizer.SyntheticTrajectory(sidewalk3, sidewalk4, crossing34);
                var movementCrossing34Walk = new Movement("Sidewalk 3", "Sidewalk 4", Turn.Crossing, ObjectType.Person, crossing34Walk.StateHistory,0);
                TrajectoryPrototypes.Add(movementCrossing34Walk);
                var t11 = new TrajectoryLogger(movementCrossing34Walk);
                t11.Save(filepath);

                var crossing43Walk = PolygonTrajectorySynthesizer.SyntheticTrajectory(sidewalk4, sidewalk3, crossing43);
                var movementCrossing43Walk = new Movement("Sidewalk 4", "Sidewalk 3", Turn.Crossing, ObjectType.Person, crossing43Walk.StateHistory,0);
                TrajectoryPrototypes.Add(movementCrossing43Walk);
                var t43 = new TrajectoryLogger(movementCrossing43Walk);
                t43.Save(filepath);
            }

            if ((crossing41 != null) && (crossing14 != null))
            {
                var crossing41Walk = PolygonTrajectorySynthesizer.SyntheticTrajectory(sidewalk4, sidewalk1, crossing41);
                var movementCrossing41Walk = new Movement("Sidewalk 4", "Sidewalk 1", Turn.Crossing, ObjectType.Person, crossing41Walk.StateHistory,0);
                TrajectoryPrototypes.Add(movementCrossing41Walk);
                var t11 = new TrajectoryLogger(movementCrossing41Walk);
                t11.Save(filepath);

                var crossing14Walk = PolygonTrajectorySynthesizer.SyntheticTrajectory(sidewalk1, sidewalk4, crossing14);
                var movementCrossing14Walk = new Movement("Sidewalk 1", "Sidewalk 4", Turn.Crossing, ObjectType.Person, crossing14Walk.StateHistory,0);
                TrajectoryPrototypes.Add(movementCrossing14Walk);
                var t12 = new TrajectoryLogger(movementCrossing14Walk);
                t12.Save(filepath);
            }
        }

        List<Movement> ApproachExitPairToMovements(Polygon approach, Polygon exit, Turn turnType)
        {
            var movements = new List<Movement>();

            //Build list of possible approach points
            var approachVertexList = approach.ToList();
            var approachCentroid = new System.Drawing.Point();
            approachCentroid.X = approach.Centroid.X;
            approachCentroid.Y = approach.Centroid.Y;
            approachVertexList.Add(approachCentroid);

            //Build list of possible exit points
            var exitVertexList = exit.ToList();
            var exitCentroid = new System.Drawing.Point();
            exitCentroid.X = exit.Centroid.X;
            exitCentroid.Y = exit.Centroid.Y;
            exitVertexList.Add(exitCentroid);

            foreach(var approachVertex in approachVertexList)
            { 
                foreach(var exitVertex in exitVertexList)
                {
                    var roadline = new RoadLine();
                    roadline.ApproachCentroidX = approachVertex.X;
                    roadline.ApproachCentroidY = approachVertex.Y;
                    roadline.ExitCentroidX = exitVertex.X;
                    roadline.ExitCentroidY = exitVertex.Y;

                    var trackedObject = PolygonTrajectorySynthesizer.SyntheticTrajectory(approachVertex, exitVertex, roadline);
                    var movement = new Movement(approach.DisplayName, exit.DisplayName, turnType, ObjectType.Car, trackedObject.StateHistory,0);
                    movements.Add(movement);
                }
            }

            return movements;
        }

        List<Movement> RoadPairToTurnMovements(Polygon approach1, Polygon exit1, Polygon approach2, Polygon exit2, Turn turnType)
        { 
             var movements = new List<Movement>();

            //Build lists of possible approach points
            var approach1VertexList = approach1.ToList();
            var approach1Centroid = new System.Drawing.Point();
            approach1Centroid.X = approach1.Centroid.X;
            approach1Centroid.Y = approach1.Centroid.Y;
            approach1VertexList.Add(approach1Centroid);

            var approach2VertexList = approach2.ToList();
            var approach2Centroid = new System.Drawing.Point();
            approach2Centroid.X = approach2.Centroid.X;
            approach2Centroid.Y = approach2.Centroid.Y;
            approach2VertexList.Add(approach2Centroid);

            //Build list of possible exit points
            var exit1VertexList = exit1.ToList();
            var exit1Centroid = new System.Drawing.Point();
            exit1Centroid.X = exit1.Centroid.X;
            exit1Centroid.Y = exit1.Centroid.Y;
            exit1VertexList.Add(exit1Centroid);

            var exit2VertexList = exit2.ToList();
            var exit2Centroid = new System.Drawing.Point();
            exit2Centroid.X = exit2.Centroid.X;
            exit2Centroid.Y = exit2.Centroid.Y;
            exit2VertexList.Add(exit2Centroid);

            foreach(var approachVertex in approach1VertexList)
            { 
                foreach(var exitVertex in exit2VertexList)
                {
                    var approachRoadlines = GenerateRoadlinesFromVertex(approachVertex,exit2VertexList);
                    var exitRoadlines = GenerateRoadlinesToVertex(approach1VertexList, exitVertex);
                    foreach(var approachRoadline in approachRoadlines)
                    { 
                        foreach(var exitRoadline in exitRoadlines)
                        { 
                            var trackedObject = PolygonTrajectorySynthesizer.SyntheticTrajectory(approachVertex, exitVertex, approachRoadline, exitRoadline);
                            var movement = new Movement(approach1.DisplayName, exit2.DisplayName, turnType, ObjectType.Car, trackedObject.StateHistory,0);
                            movements.Add(movement);
                        }
                    }
                }
            }

            return movements;
        }

        List<RoadLine> GenerateRoadlinesFromVertex(System.Drawing.Point vertex, List<System.Drawing.Point> matchVertices)
        {
            var roadlines = new List<RoadLine>();
            foreach(var mv in matchVertices)
            {
                var roadline = new RoadLine();
                roadline.ApproachCentroidX = vertex.X;
                roadline.ApproachCentroidY = vertex.Y;
                roadline.ExitCentroidX = mv.X;
                roadline.ExitCentroidY = mv.Y;
                roadlines.Add(roadline);
            }
            return roadlines;
        }

        List<RoadLine> GenerateRoadlinesToVertex(List<System.Drawing.Point> matchVertices, System.Drawing.Point vertex)
        {
            var roadlines = new List<RoadLine>();
            foreach(var mv in matchVertices)
            {
                var roadline = new RoadLine();
                roadline.ApproachCentroidX = mv.X;
                roadline.ApproachCentroidY = mv.Y;
                roadline.ExitCentroidX = vertex.X;
                roadline.ExitCentroidY = vertex.Y;
                roadlines.Add(roadline);
            }
            return roadlines;
        }
    }
}
