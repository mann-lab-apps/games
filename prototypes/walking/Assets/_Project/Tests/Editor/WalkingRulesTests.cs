using MannLab.Games.Walking;
using NUnit.Framework;
using UnityEngine;

namespace MannLab.Games.Walking.Tests
{
    public sealed class WalkingRulesTests
    {
        [Test]
        public void GeneratedMazeClearsStartArea()
        {
            var maze = WalkingMaze.Generate(11, 18, 1234, WalkingRules.TileSize);

            for (var y = 1; y <= 12; y++)
            {
                for (var x = 1; x <= 5; x++)
                {
                    Assert.That(maze.IsSolidGrid(x, y), Is.False, $"Start tile {x},{y} should be open.");
                }
            }
        }

        [Test]
        public void BodyCollisionDetectsWallTiles()
        {
            var solid = new bool[5, 5];
            solid[2, 2] = true;
            var maze = WalkingMaze.CreateForTests(solid, 1f);

            Assert.That(WalkingRules.IsBodyColliding(maze.GridToWorld(2, 2), maze), Is.True);
            Assert.That(WalkingRules.IsBodyColliding(maze.GridToWorld(1, 1), maze), Is.False);
        }

        [Test]
        public void FootPlacementRejectsOverlongSteps()
        {
            var maze = EmptyMaze();
            var placement = WalkingRules.ValidateFootPlacement(
                WalkingFootSide.Right,
                Vector2.zero,
                Vector2.up * (WalkingRules.MaxStepDistance + 0.4f) + Vector2.right * WalkingRules.NaturalHalfStance,
                Vector2.up,
                maze);

            Assert.That(placement.IsValid, Is.False);
            Assert.That(placement.Reason, Is.EqualTo("long"));
        }

        [Test]
        public void FootPlacementRejectsCrossedFeet()
        {
            var maze = EmptyMaze();
            var placement = WalkingRules.ValidateFootPlacement(
                WalkingFootSide.Left,
                Vector2.zero,
                Vector2.up * 0.72f + Vector2.right * 0.2f,
                Vector2.up,
                maze);

            Assert.That(placement.IsValid, Is.False);
            Assert.That(placement.Reason, Is.EqualTo("cross"));
        }

        [Test]
        public void FootPlacementRejectsWallLandings()
        {
            var solid = new bool[7, 7];
            solid[3, 4] = true;
            var maze = WalkingMaze.CreateForTests(solid, 1f);
            var wallPosition = maze.GridToWorld(3, 4);
            var placement = WalkingRules.ValidateFootPlacement(
                WalkingFootSide.Right,
                wallPosition - Vector2.up * 0.68f - Vector2.right * WalkingRules.NaturalHalfStance,
                wallPosition,
                Vector2.up,
                maze);

            Assert.That(placement.IsValid, Is.False);
            Assert.That(placement.Reason, Is.EqualTo("wall"));
        }

        [Test]
        public void ReturnGestureOnlyStartsNearBodySideOfScreen()
        {
            var screen = new Vector2(1080f, 1920f);

            Assert.That(WalkingRules.IsReturnGesturePosition(new Vector2(540f, 420f), screen), Is.True);
            Assert.That(WalkingRules.IsReturnGesturePosition(new Vector2(540f, 700f), screen), Is.False);
            Assert.That(WalkingRules.IsReturnGesturePosition(new Vector2(540f, 1240f), screen), Is.False);
        }

        [Test]
        public void MiddleScreenTouchCanBuildValidStep()
        {
            var screen = new Vector2(1080f, 1920f);
            var candidate = WalkingRules.BuildFootCandidate(
                WalkingFootSide.Right,
                Vector2.zero,
                Vector2.up,
                new Vector2(810f, 960f),
                screen);
            var placement = WalkingRules.ValidateFootPlacement(
                WalkingFootSide.Right,
                Vector2.zero,
                candidate,
                Vector2.up,
                EmptyMaze());

            Assert.That(WalkingRules.IsStepGesturePosition(new Vector2(810f, 960f), screen), Is.True);
            Assert.That(placement.IsValid, Is.True);
        }

        [Test]
        public void RightHalfScreenTargetsRightFoot()
        {
            var screen = new Vector2(1080f, 1920f);

            Assert.That(WalkingRules.FootSideForScreenPosition(new Vector2(260f, 960f), screen), Is.EqualTo(WalkingFootSide.Left));
            Assert.That(WalkingRules.FootSideForScreenPosition(new Vector2(540f, 960f), screen), Is.EqualTo(WalkingFootSide.Right));
            Assert.That(WalkingRules.FootSideForScreenPosition(new Vector2(820f, 960f), screen), Is.EqualTo(WalkingFootSide.Right));
        }

        [Test]
        public void ReturnAreaCanStillStartStepWhenReturnIsNotRequired()
        {
            var screen = new Vector2(1080f, 1920f);

            Assert.That(WalkingRules.IsReturnGesturePosition(new Vector2(240f, 420f), screen), Is.True);
            Assert.That(WalkingRules.IsStepGesturePosition(new Vector2(240f, 420f), screen), Is.True);
        }

        [Test]
        public void DefaultPlayModeHidesFootMarkers()
        {
            Assert.That(WalkingController.DefaultDebugFootMarkers, Is.False);
        }

        private static WalkingMaze EmptyMaze()
        {
            return WalkingMaze.CreateForTests(new bool[9, 9], 1f);
        }
    }
}
