using Hollow.Input;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Hollow.Tests.EditMode
{
    public sealed class ManualAimInputReaderTests : InputTestFixture
    {
        [SetUp]
        public override void Setup()
        {
            base.Setup();
            GameplayInputReader.SetExternalMoveOverride(Vector2.zero);
        }

        [TearDown]
        public override void TearDown()
        {
            GameplayInputReader.SetExternalMoveOverride(Vector2.zero);
            base.TearDown();
        }

        [Test]
        public void KeyboardLightAttackProducesSnapshotPress()
        {
            var keyboard = InputSystem.AddDevice<Keyboard>();

            Press(keyboard.jKey);

            var input = GameplayInputReader.ReadCurrent();
            Assert.IsTrue(input.LightAttackPressed);
            Assert.IsTrue(input.LightAttackHeld);
        }

        [Test]
        public void MouseLightAttackPreservesPointerAndAimIntent()
        {
            var mouse = InputSystem.AddDevice<Mouse>();
            Set(mouse.position, new Vector2(640f, 360f));
            Set(mouse.delta, new Vector2(4f, 2f));

            Press(mouse.leftButton);

            var input = GameplayInputReader.ReadCurrent();
            Assert.IsTrue(input.LightAttackPressed);
            Assert.IsTrue(input.LightAttackHeld);
            Assert.IsTrue(input.HasPointerScreenPosition);
            Assert.IsTrue(input.MouseAimIntent);
            Assert.AreEqual(640f, input.PointerScreenPosition.x, 0.001f);
            Assert.AreEqual(360f, input.PointerScreenPosition.y, 0.001f);
        }

        [Test]
        public void GamepadRightShoulderAndRightStickProduceAttackAndAimSnapshot()
        {
            var gamepad = InputSystem.AddDevice<Gamepad>();
            var aim = new Vector2(0.7f, 0.35f);
            Set(gamepad.rightStick, aim);

            Press(gamepad.rightShoulder);

            var input = GameplayInputReader.ReadCurrent();
            var expected = aim.normalized;
            Assert.IsTrue(input.LightAttackPressed);
            Assert.IsTrue(input.LightAttackHeld);
            Assert.AreEqual(expected.x, input.Shoot.x, 0.001f);
            Assert.AreEqual(expected.y, input.Shoot.y, 0.001f);
        }
    }
}
