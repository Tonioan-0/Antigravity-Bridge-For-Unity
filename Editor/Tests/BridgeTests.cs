using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using AntigravityBridge.Editor;
using AntigravityBridge.Editor.Models;
using UnityEditor;

namespace AntigravityBridge.Editor.Tests
{
    public class BridgeTests
    {
        [SetUp]
        public void Setup()
        {
            // Cleanup before each test
            var obj = GameObject.Find("TestObject");
            if (obj != null) Object.DestroyImmediate(obj);

            var parent = GameObject.Find("TestParent");
            if (parent != null) Object.DestroyImmediate(parent);
        }

        [TearDown]
        public void Teardown()
        {
            // Cleanup after each test
            var obj = GameObject.Find("TestObject");
            if (obj != null) Object.DestroyImmediate(obj);

            var parent = GameObject.Find("TestParent");
            if (parent != null) Object.DestroyImmediate(parent);
        }

        [Test]
        public void CreateObject_CreatesGameObject()
        {
            // Arrange
            string json = "{\"name\":\"TestObject\",\"position\":{\"x\":1,\"y\":2,\"z\":3}}";

            // Act
            var response = CommandExecutor.CreateObject(json);

            // Assert
            Assert.AreEqual("success", response.status);

            var obj = GameObject.Find("TestObject");
            Assert.IsNotNull(obj);
            Assert.AreEqual(new Vector3(1, 2, 3), obj.transform.position);
        }

        [Test]
        public void CreateObject_WithComponents_AddsComponents()
        {
            // Arrange
            string json = "{\"name\":\"TestObject\",\"components\":[\"BoxCollider\",\"Rigidbody\"]}";

            // Act
            CommandExecutor.CreateObject(json);

            // Assert
            var obj = GameObject.Find("TestObject");
            Assert.IsNotNull(obj);
            Assert.IsNotNull(obj.GetComponent<BoxCollider>());
            Assert.IsNotNull(obj.GetComponent<Rigidbody>());
        }

        [Test]
        public void FindObjects_FindsByName()
        {
            // Arrange
            new GameObject("TestObject");
            string json = "{\"filter\":{\"type\":\"name\",\"value\":\"TestObject\"}}";

            // Act
            var response = CommandExecutor.FindObjects(json);

            // Assert
            Assert.AreEqual("success", response.status);
            Assert.AreEqual(1, response.data.count);
            Assert.AreEqual("TestObject", response.data.affected_objects[0]);
        }

        [Test]
        public void ModifyComponent_ChangesProperties()
        {
            // Arrange
            var obj = new GameObject("TestObject");
            obj.AddComponent<BoxCollider>();
            string compJson = "{\"objects\":[\"TestObject\"],\"component\":\"BoxCollider\",\"propertyValues\":[{\"key\":\"isTrigger\",\"boolValue\":true,\"valueType\":\"bool\"}]}";

            // Act
            CommandExecutor.ModifyComponent(compJson);

            // Assert
            Assert.IsTrue(obj.GetComponent<BoxCollider>().isTrigger);
        }

        [Test]
        public void SceneQuery_GetHierarchy_ReturnsRootObjects()
        {
            // Arrange
            new GameObject("TestObject");

            // Act
            var response = SceneQueryAPI.GetSceneHierarchy();

            // Assert
            Assert.AreEqual("success", response.status);
            Assert.IsNotNull(response.data.scene_hierarchy);
            Assert.IsTrue(response.data.scene_hierarchy.root_objects.Length > 0);
        }
    }
}
