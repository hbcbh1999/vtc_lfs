using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VTC.Common;
using VTC.Common.RegionConfig;
using VTC.Reporting;
using System.Threading;

namespace VTC.Kernel
{
    public class MultipleTrajectorySynthesizer
    {

        public List<Movement> TrajectoryPrototypes = new List<Movement>();

        public void GenerateSyntheticTrajectories(RegionConfig regionConfig, string filepath)
        {
            Console.WriteLine("Generating synthetic trajectories...");

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

            //TODO: Name assignment below is kind of a hack, should not be necessary.
            approach1.DisplayName = "Approach 1";
            approach2.DisplayName = "Approach 2";
            approach3.DisplayName = "Approach 3";
            approach4.DisplayName = "Approach 4";

            exit1.DisplayName = "Exit 1";
            exit2.DisplayName = "Exit 2";
            exit3.DisplayName = "Exit 3";
            exit4.DisplayName = "Exit 4";

            sidewalk1.DisplayName = "Sidewalk 1";
            sidewalk2.DisplayName = "Sidewalk 2";
            sidewalk3.DisplayName = "Sidewalk 3";
            sidewalk4.DisplayName = "Sidewalk 4";

            //Generate Straight movements
            var straightMovements = new List<Movement>();

            var road1StraightMovements = ApproachExitPairToMovements(approach1, exit1, Turn.Straight, ObjectType.Car, 50);
            straightMovements.AddRange(road1StraightMovements);

            var road2StraightMovements = ApproachExitPairToMovements(approach2, exit2, Turn.Straight, ObjectType.Car, 50);
            straightMovements.AddRange(road2StraightMovements);

            var road3StraightMovements = ApproachExitPairToMovements(approach3, exit3, Turn.Straight, ObjectType.Car, 50);
            straightMovements.AddRange(road3StraightMovements);

            var road4StraightMovements = ApproachExitPairToMovements(approach4, exit4, Turn.Straight, ObjectType.Car, 50);
            straightMovements.AddRange(road4StraightMovements);
            
            foreach(var m in straightMovements)
            { 
                TrajectoryPrototypes.Add(m);
                var tl = new TrajectoryLogger(m);
                tl.Save(filepath);  
            }

            //Generate turn movements
            var turnMovements = new List<Movement>();

            var road12Movements = RoadPairToTurnMovements(approach1, exit1, approach2, exit2,Turn.Left, ObjectType.Car, 12);
            turnMovements.AddRange(road12Movements);
            var road21Movements = RoadPairToTurnMovements(approach2, exit2, approach1, exit1,Turn.Right, ObjectType.Car, 12);
            turnMovements.AddRange(road21Movements);

            var road23Movements = RoadPairToTurnMovements(approach2, exit2, approach3, exit3,Turn.Left, ObjectType.Car, 12);
            turnMovements.AddRange(road23Movements);
            var road32Movements = RoadPairToTurnMovements(approach3, exit3, approach2, exit2,Turn.Right, ObjectType.Car, 12);
            turnMovements.AddRange(road32Movements);

            var road34Movements = RoadPairToTurnMovements(approach3, exit3, approach4, exit4,Turn.Left, ObjectType.Car, 12);
            turnMovements.AddRange(road34Movements);
            var road43Movements = RoadPairToTurnMovements(approach4, exit4, approach3, exit3,Turn.Right, ObjectType.Car, 12);
            turnMovements.AddRange(road43Movements);

            var road41Movements = RoadPairToTurnMovements(approach4, exit4, approach1, exit1,Turn.Left, ObjectType.Car, 12);
            turnMovements.AddRange(road41Movements);
            var road14Movements = RoadPairToTurnMovements(approach1, exit1, approach4, exit4,Turn.Right, ObjectType.Car, 12);
            turnMovements.AddRange(road14Movements);

            foreach(var m in turnMovements)
            { 
                TrajectoryPrototypes.Add(m);
                var tl = new TrajectoryLogger(m);
                tl.Save(filepath);  
            }

            //Generate pedestrian Crossing movements
            var pedestrianMovements = new List<Movement>();

            var crossing12Movements = ApproachExitPairToMovements(sidewalk1, sidewalk2, Turn.Crossing, ObjectType.Person, 4);
            pedestrianMovements.AddRange(crossing12Movements);

            var crossing21Movements = ApproachExitPairToMovements(sidewalk2, sidewalk1, Turn.Crossing, ObjectType.Person, 4);
            pedestrianMovements.AddRange(crossing21Movements);

            var crossing23Movements = ApproachExitPairToMovements(sidewalk2, sidewalk3, Turn.Crossing, ObjectType.Person, 4);
            pedestrianMovements.AddRange(crossing23Movements);

            var crossing32Movements = ApproachExitPairToMovements(sidewalk3, sidewalk2, Turn.Crossing, ObjectType.Person, 4);
            pedestrianMovements.AddRange(crossing32Movements);

            var crossing34Movements = ApproachExitPairToMovements(sidewalk3, sidewalk4, Turn.Crossing, ObjectType.Person, 4);
            pedestrianMovements.AddRange(crossing34Movements);

            var crossing43Movements = ApproachExitPairToMovements(sidewalk4, sidewalk3, Turn.Crossing, ObjectType.Person, 4);
            pedestrianMovements.AddRange(crossing43Movements);

            var crossing14Movements = ApproachExitPairToMovements(sidewalk1, sidewalk4, Turn.Crossing, ObjectType.Person, 4);
            pedestrianMovements.AddRange(crossing14Movements);

            var crossing41Movements = ApproachExitPairToMovements(sidewalk4, sidewalk1, Turn.Crossing, ObjectType.Person, 4);
            pedestrianMovements.AddRange(crossing41Movements);

            foreach(var m in pedestrianMovements)
            { 
                TrajectoryPrototypes.Add(m);
                var tl = new TrajectoryLogger(m);
                tl.Save(filepath);  
            }

            Console.WriteLine("Synthetic trajectory generation finished.");
        }

        List<Movement> ApproachExitPairToMovements(Polygon approach, Polygon exit, Turn turnType, ObjectType objectType, int max)
        {
            var movements = new ConcurrentBag<Movement>();

            if(approach.Count < 1 || exit.Count < 1)
            {
                return movements.ToList();
            }

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

            Parallel.ForEach(approachVertexList, approachVertex => 
            { 
                foreach(var exitVertex in exitVertexList)
                {
                    var roadline = new RoadLine();
                    roadline.ApproachCentroidX = approachVertex.X;
                    roadline.ApproachCentroidY = approachVertex.Y;
                    roadline.ExitCentroidX = exitVertex.X;
                    roadline.ExitCentroidY = exitVertex.Y;

                    var trackedObject = PolygonTrajectorySynthesizer.SyntheticTrajectory(approachVertex, exitVertex, roadline);
                    var movement = new Movement(approach.DisplayName, exit.DisplayName, turnType, objectType, trackedObject.StateHistory,0);
                    movements.Add(movement);
                }
            });

            var movementsList = movements.ToList();
            MyExtensions.Shuffle(movementsList);
            var movementsListReduced = movementsList.Take(max).ToList();
            return movementsListReduced;
        }

        List<Movement> RoadPairToTurnMovements(Polygon approachA, Polygon exitA, Polygon approachB, Polygon exitB, Turn turnType, ObjectType objectType, int max)
        { 
             var movements = new ConcurrentBag<Movement>();

            if(approachA.Count < 1 || exitA.Count < 1 || approachB.Count < 1 || exitB.Count < 1)
            {
                return movements.ToList();
            }

            //Build lists of possible approach points
            var approachAVertexList = approachA.ToList();
            var approachACentroid = new System.Drawing.Point();
            approachACentroid.X = approachA.Centroid.X;
            approachACentroid.Y = approachA.Centroid.Y;
            approachAVertexList.Add(approachACentroid);

            var approachBVertexList = approachB.ToList();
            var approachBCentroid = new System.Drawing.Point();
            approachBCentroid.X = approachB.Centroid.X;
            approachBCentroid.Y = approachB.Centroid.Y;
            approachBVertexList.Add(approachBCentroid);

            //Build list of possible exit points
            var exitAVertexList = exitA.ToList();
            var exitACentroid = new System.Drawing.Point();
            exitACentroid.X = exitA.Centroid.X;
            exitACentroid.Y = exitA.Centroid.Y;
            exitAVertexList.Add(exitACentroid);

            var exitBVertexList = exitB.ToList();
            var exitBCentroid = new System.Drawing.Point();
            exitBCentroid.X = exitB.Centroid.X;
            exitBCentroid.Y = exitB.Centroid.Y;
            exitBVertexList.Add(exitBCentroid);

            Parallel.ForEach(approachAVertexList, approachVertex => 
            { 
                foreach(var exitVertex in exitBVertexList)
                {
                    var approachRoadlines = GenerateRoadlinesFromVertex(approachVertex,exitAVertexList);
                    var exitRoadlines = GenerateRoadlinesToVertex(approachBVertexList, exitVertex);
                    foreach(var approachRoadline in approachRoadlines)
                    { 
                        foreach(var exitRoadline in exitRoadlines)
                        { 
                            var trackedObject = PolygonTrajectorySynthesizer.SyntheticTrajectory(approachVertex, exitVertex, approachRoadline, exitRoadline);
                            var movement = new Movement(approachA.DisplayName, exitB.DisplayName, turnType, objectType, trackedObject.StateHistory,0);
                            movements.Add(movement);
                        }
                    }
                }
            });
            
            var movementsList = movements.ToList();
            MyExtensions.Shuffle(movementsList);
            var movementsListReduced = movementsList.Take(max).ToList();
            return movementsListReduced;
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

  public static class ThreadSafeRandom
  {
      [ThreadStatic] private static Random Local;

      public static Random ThisThreadsRandom
      {
        //Note: We explicitly do not want a random seed here. We want a pseudo-random shuffle of the list so that count results are the same
        //from one run to the next, one PC to the next, etc.
          get { return Local ?? (Local = new Random(0)); }
      }
  }

  static class MyExtensions
  {
    public static void Shuffle<T>(this IList<T> list)
    {
      int n = list.Count;
      while (n > 1)
      {
        n--;
        int k = ThreadSafeRandom.ThisThreadsRandom.Next(n + 1);
        T value = list[k];
        list[k] = list[n];
        list[n] = value;
      }
    }
  }
}
