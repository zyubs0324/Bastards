using System;
using NUnit.Framework;
using UnityEngine;

namespace TAoB.Data.Tests
{
    /// <summary>
    /// 로더가 기대는 JsonUtility 의 동작을 고정한다(계획서 Phase 3: 누락 필드의 초기값 유지 여부는 첫 테스트로 확인).
    /// 엔진 버전이 올라가 동작이 바뀌면 여기서 먼저 깨진다.
    /// </summary>
    public class JsonUtilityBehaviourTests
    {
        [Serializable]
        private class Root
        {
            public int present = -1;
            public int missing = -1;
            public Child[] children;
            public Child missingChild;
        }

        [Serializable]
        private class Child
        {
            public int present = -1;
            public int missing = -1;
        }

        [Test]
        public void MissingField_KeepsInitializer_AtRoot()
        {
            var root = JsonUtility.FromJson<Root>("{\"present\":1,\"children\":[]}");

            Assert.AreEqual(1, root.present);
            Assert.AreEqual(-1, root.missing);
        }

        [Test]
        public void MissingField_KeepsInitializer_InArrayElement()
        {
            var root = JsonUtility.FromJson<Root>("{\"present\":1,\"children\":[{\"present\":2}]}");

            Assert.AreEqual(2, root.children[0].present);
            Assert.AreEqual(-1, root.children[0].missing);
        }

        [Test]
        public void MissingArray_IsNull()
        {
            var root = JsonUtility.FromJson<Root>("{\"present\":1}");

            Assert.IsNull(root.children);
        }

        [Test]
        public void MissingObject_IsCreatedWithInitializers()
        {
            // 빠진 객체는 null 이 아니라 새로 만들어진다. 안의 필수 필드가 -1 로 남으므로 필드 검사가 누락을 잡는다.
            var root = JsonUtility.FromJson<Root>("{\"present\":1}");

            Assert.IsNotNull(root.missingChild);
            Assert.AreEqual(-1, root.missingChild.missing);
        }

        [Test]
        public void MalformedJson_Throws()
        {
            Assert.Throws<ArgumentException>(() => JsonUtility.FromJson<Root>("{\"present\":"));
        }
    }
}
