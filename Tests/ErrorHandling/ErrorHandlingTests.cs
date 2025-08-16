using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using NeonQuest.Core.ErrorHandling;

namespace NeonQuest.Tests.ErrorHandling
{
    public class ErrorHandlingTests
    {
        private GameObject _testGameObject;
        private ErrorBoundary _errorBoundary;

        [SetUp]
        public void SetUp()
        {
            _testGameObject = new GameObject("TestErrorBoundary");
            _errorBoundary = _testGameObject.AddComponent<ErrorBoundary>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testGameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_testGameObject);
            }
        }

        [Test]
        public void ErrorBoundary_SuccessfulOperation_ReturnsTrue()
        {
            // Arrange
            bool operationExecuted = false;
            Action testAction = () => { operationExecuted = true; };

            // Act
            bool result = _errorBoundary.TryExecute(testAction, "TestOperation");

            // Assert
            Assert.IsTrue(result);
            Assert.IsTrue(operationExecuted);
        }

        [Test]
        public void ErrorBoundary_ExceptionThrown_ReturnsFalse()
        {
            // Arrange
            Action throwingAction = () => { throw new System.Exception("Test exception"); };

            // Act
            bool result = _errorBoundary.TryExecute(throwingAction, "ThrowingOperation");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void ErrorBoundary_FunctionWithException_ReturnsDefaultValue()
        {
            // Arrange
            Func<int> throwingFunction = () => { throw new System.Exception("Test exception"); };
            int defaultValue = 42;

            // Act
            int result = _errorBoundary.TryExecute(throwingFunction, defaultValue, "ThrowingFunction");

            // Assert
            Assert.AreEqual(defaultValue, result);
        }

        [Test]
        public void ErrorBoundary_SuccessfulFunction_ReturnsCorrectValue()
        {
            // Arrange
            int expectedValue = 123;
            Func<int> successFunction = () => expectedValue;

            // Act
            int result = _errorBoundary.TryExecute(successFunction, 0, "SuccessFunction");

            // Assert
            Assert.AreEqual(expectedValue, result);
        }

        [Test]
        public void ErrorBoundary_MultipleErrors_DisablesAfterMaxErrors()
        {
            // Arrange
            Action throwingAction = () => { throw new System.Exception("Test exception"); };

            // Act - Execute multiple failing operations
            for (int i = 0; i < 6; i++) // More than MAX_ERRORS_BEFORE_DISABLE (5)
            {
                _errorBoundary.TryExecute(throwingAction, $"ThrowingOperation_{i}");
            }

            // Assert
            Assert.IsFalse(_errorBoundary.enabled);
        }

        [Test]
        public void ErrorBoundary_ResetAfterErrors_EnablesComponent()
        {
            // Arrange
            Action throwingAction = () => { throw new System.Exception("Test exception"); };
            
            // Generate errors to disable component
            for (int i = 0; i < 6; i++)
            {
                _errorBoundary.TryExecute(throwingAction, $"ThrowingOperation_{i}");
            }

            // Act
            _errorBoundary.ResetErrorBoundary();

            // Assert
            Assert.IsTrue(_errorBoundary.enabled);
        }

        [Test]
        public void FallbackBehaviors_GetDefaultConfigValue_ReturnsCorrectValue()
        {
            // Act
            float generationDistance = FallbackBehaviors.GetDefaultConfigValue<float>("generation_distance");
            float nonExistentValue = FallbackBehaviors.GetDefaultConfigValue<float>("non_existent_key", 99.0f);

            // Assert
            Assert.AreEqual(50.0f, generationDistance);
            Assert.AreEqual(99.0f, nonExistentValue);
        }

        [Test]
        public void FallbackBehaviors_GetFallbackPrefab_ReturnsValidPrefab()
        {
            // Act
            GameObject fallbackPrefab = FallbackBehaviors.GetFallbackPrefab();

            // Assert
            Assert.IsNotNull(fallbackPrefab);
            Assert.AreEqual("FallbackPrefab", fallbackPrefab.name);
        }

        [Test]
        public void FallbackBehaviors_GetFallbackMaterial_ReturnsValidMaterial()
        {
            // Act
            Material fallbackMaterial = FallbackBehaviors.GetFallbackMaterial();

            // Assert
            Assert.IsNotNull(fallbackMaterial);
            Assert.AreEqual("FallbackMaterial", fallbackMaterial.name);
            Assert.AreEqual(Color.magenta, fallbackMaterial.color);
        }

        [Test]
        public void FallbackBehaviors_GetSafeGenerationParameters_ReturnsValidParameters()
        {
            // Act
            GenerationParameters safeParams = FallbackBehaviors.GetSafeGenerationParameters();

            // Assert
            Assert.IsNotNull(safeParams);
            Assert.AreEqual(1, safeParams.MaxObjectsPerFrame);
            Assert.AreEqual(25.0f, safeParams.GenerationDistance);
            Assert.IsTrue(safeParams.UseSimplifiedGeometry);
            Assert.IsTrue(safeParams.DisableComplexEffects);
        }

        [Test]
        public void NeonQuestLogger_LogLevels_WorkCorrectly()
        {
            // This test verifies that different log levels can be called without exceptions
            // In a real scenario, you'd want to capture and verify the actual log output
            
            // Act & Assert (no exceptions should be thrown)
            Assert.DoesNotThrow(() => NeonQuestLogger.LogDebug("Debug message"));
            Assert.DoesNotThrow(() => NeonQuestLogger.LogInfo("Info message"));
            Assert.DoesNotThrow(() => NeonQuestLogger.LogWarning("Warning message"));
            Assert.DoesNotThrow(() => NeonQuestLogger.LogError("Error message"));
            Assert.DoesNotThrow(() => NeonQuestLogger.LogCritical("Critical message"));
        }

        [Test]
        public void NeonQuestLogger_LogException_HandlesExceptionCorrectly()
        {
            // Arrange
            var testException = new System.InvalidOperationException("Test exception message");

            // Act & Assert (no exceptions should be thrown)
            Assert.DoesNotThrow(() => NeonQuestLogger.LogException(testException));
        }
    }
}