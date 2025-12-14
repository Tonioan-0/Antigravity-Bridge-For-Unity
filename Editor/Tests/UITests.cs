using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.TestTools;
using AntigravityBridge.Editor;
using AntigravityBridge.Editor.Models;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace AntigravityBridge.Editor.Tests
{
    public class UITests
    {
        [SetUp]
        public void Setup()
        {
            // SAFETY: Always start with a fresh empty scene to avoid deleting user data
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        [TearDown]
        public void Teardown()
        {
            // Optional: Clean up if needed, but since we create a new scene in SetUp, it's safer.
            // We can also ensure we don't save the test scene.
        }

        [Test]
        public void CreateCanvas_CreatesCanvasAndEventSystem()
        {
            // Arrange
            string json = "{\"type\":\"canvas\",\"name\":\"TestCanvas\"}";

            // Act
            var response = UIAPI.CreateUIElement(json);

            // Assert
            Assert.AreEqual("success", response.status);
            var canvas = GameObject.Find("TestCanvas");
            Assert.IsNotNull(canvas);
            Assert.IsNotNull(canvas.GetComponent<Canvas>());
            Assert.IsNotNull(canvas.GetComponent<CanvasScaler>());
            Assert.IsNotNull(canvas.GetComponent<GraphicRaycaster>());

            // Check EventSystem
            var es = Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
            Assert.IsNotNull(es);
        }

        [Test]
        public void CreateButton_CreatesButtonInCanvas()
        {
            // Arrange - First create canvas
            UIAPI.CreateUIElement("{\"type\":\"canvas\",\"name\":\"MyCanvas\"}");

            // Act - Create button
            string json = "{\"type\":\"button\",\"name\":\"MyButton\",\"parent\":\"MyCanvas\",\"text\":\"Click Me\"}";
            var response = UIAPI.CreateUIElement(json);

            // Assert
            Assert.AreEqual("success", response.status);
            var btnObj = GameObject.Find("MyButton");
            Assert.IsNotNull(btnObj);
            Assert.IsNotNull(btnObj.GetComponent<Button>());
            Assert.IsNotNull(btnObj.GetComponent<Image>());
            Assert.AreEqual("MyCanvas", btnObj.transform.parent.name);

            // Check text child
            var textObj = btnObj.transform.Find("Text");
            Assert.IsNotNull(textObj);
            Assert.AreEqual("Click Me", textObj.GetComponent<Text>().text);
        }

        [Test]
        public void ModifyRectTransform_ChangesAnchors()
        {
            // Arrange
            UIAPI.CreateUIElement("{\"type\":\"canvas\",\"name\":\"TestCanvas\"}");
            UIAPI.CreateUIElement("{\"type\":\"panel\",\"name\":\"TestPanel\",\"parent\":\"TestCanvas\"}");

            // Act
            string json = "{\"objects\":[\"TestPanel\"],\"preset\":\"top-left\"}";
            var response = UIAPI.ModifyRectTransform(json);

            // Assert
            Assert.AreEqual("success", response.status);
            var panel = GameObject.Find("TestPanel").GetComponent<RectTransform>();
            Assert.AreEqual(new Vector2(0, 1), panel.anchorMin);
            Assert.AreEqual(new Vector2(0, 1), panel.anchorMax);
            Assert.AreEqual(new Vector2(0, 1), panel.pivot);
        }
    }
}
