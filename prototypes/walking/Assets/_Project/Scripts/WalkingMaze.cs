using System;
using System.Collections.Generic;
using UnityEngine;

namespace MannLab.Games.Walking
{
    public sealed class WalkingMaze
    {
        private readonly bool[,] solid;

        private WalkingMaze(bool[,] solid, float tileSize)
        {
            this.solid = solid ?? throw new ArgumentNullException(nameof(solid));
            TileSize = tileSize;
            GridWidth = solid.GetLength(0);
            GridHeight = solid.GetLength(1);
        }

        public int GridWidth { get; }
        public int GridHeight { get; }
        public float TileSize { get; }

        public static WalkingMaze Generate(int cellColumns, int cellRows, int seed, float tileSize)
        {
            if (cellColumns < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(cellColumns));
            }

            if (cellRows < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(cellRows));
            }

            var gridWidth = cellColumns * 2 + 1;
            var gridHeight = cellRows * 2 + 1;
            var map = new bool[gridWidth, gridHeight];
            for (var y = 0; y < gridHeight; y++)
            {
                for (var x = 0; x < gridWidth; x++)
                {
                    map[x, y] = true;
                }
            }

            var random = new System.Random(seed);
            var visited = new bool[cellColumns, cellRows];
            var stack = new Stack<Vector2Int>();
            stack.Push(new Vector2Int(0, 0));
            visited[0, 0] = true;
            map[1, 1] = false;

            var directions = new[]
            {
                new Vector2Int(1, 0),
                new Vector2Int(-1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(0, -1)
            };

            while (stack.Count > 0)
            {
                var current = stack.Peek();
                Shuffle(directions, random);
                var moved = false;
                for (var i = 0; i < directions.Length; i++)
                {
                    var next = current + directions[i];
                    if (next.x < 0 || next.y < 0 || next.x >= cellColumns || next.y >= cellRows || visited[next.x, next.y])
                    {
                        continue;
                    }

                    visited[next.x, next.y] = true;
                    var currentGrid = CellToGrid(current);
                    var nextGrid = CellToGrid(next);
                    map[nextGrid.x, nextGrid.y] = false;
                    map[(currentGrid.x + nextGrid.x) / 2, (currentGrid.y + nextGrid.y) / 2] = false;
                    stack.Push(next);
                    moved = true;
                    break;
                }

                if (!moved)
                {
                    stack.Pop();
                }
            }

            ClearStartArea(map);
            return new WalkingMaze(map, tileSize);
        }

        public static WalkingMaze CreateForTests(bool[,] solid, float tileSize)
        {
            return new WalkingMaze((bool[,])solid.Clone(), tileSize);
        }

        public bool IsSolidGrid(int x, int y)
        {
            return x < 0 || y < 0 || x >= GridWidth || y >= GridHeight || solid[x, y];
        }

        public Vector2 GridToWorld(int x, int y)
        {
            return new Vector2(
                (x - (GridWidth - 1) * 0.5f) * TileSize,
                (y - (GridHeight - 1) * 0.5f) * TileSize);
        }

        public bool IsCircleTouchingWall(Vector2 center, float radius)
        {
            var min = WorldToGrid(center - Vector2.one * radius);
            var max = WorldToGrid(center + Vector2.one * radius);
            for (var y = min.y; y <= max.y; y++)
            {
                for (var x = min.x; x <= max.x; x++)
                {
                    if (!IsSolidGrid(x, y))
                    {
                        continue;
                    }

                    var tileCenter = GridToWorld(x, y);
                    var half = TileSize * 0.5f;
                    var closestX = Mathf.Clamp(center.x, tileCenter.x - half, tileCenter.x + half);
                    var closestY = Mathf.Clamp(center.y, tileCenter.y - half, tileCenter.y + half);
                    var closest = new Vector2(closestX, closestY);
                    if ((center - closest).sqrMagnitude <= radius * radius)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public IEnumerable<Vector2Int> SolidTiles()
        {
            for (var y = 0; y < GridHeight; y++)
            {
                for (var x = 0; x < GridWidth; x++)
                {
                    if (solid[x, y])
                    {
                        yield return new Vector2Int(x, y);
                    }
                }
            }
        }

        private Vector2Int WorldToGrid(Vector2 world)
        {
            return new Vector2Int(
                Mathf.FloorToInt(world.x / TileSize + GridWidth * 0.5f),
                Mathf.FloorToInt(world.y / TileSize + GridHeight * 0.5f));
        }

        private static Vector2Int CellToGrid(Vector2Int cell)
        {
            return new Vector2Int(cell.x * 2 + 1, cell.y * 2 + 1);
        }

        private static void ClearStartArea(bool[,] map)
        {
            var maxX = Mathf.Min(4, map.GetLength(0) - 2);
            var maxY = Mathf.Min(4, map.GetLength(1) - 2);
            for (var y = 1; y <= maxY; y++)
            {
                for (var x = 1; x <= maxX; x++)
                {
                    map[x, y] = false;
                }
            }
        }

        private static void Shuffle(IList<Vector2Int> values, System.Random random)
        {
            for (var i = values.Count - 1; i > 0; i--)
            {
                var swap = random.Next(i + 1);
                (values[i], values[swap]) = (values[swap], values[i]);
            }
        }
    }
}
