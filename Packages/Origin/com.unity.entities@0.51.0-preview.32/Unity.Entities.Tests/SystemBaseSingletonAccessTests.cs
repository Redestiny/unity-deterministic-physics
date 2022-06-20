#define SOME_DEFINE
using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
#pragma warning disable 649

namespace Unity.Entities.Tests
{
    partial class SystemBaseSingletonAccessTests : ECSTestsFixture
    {
        SystemBase_TestSystem TestSystem;

        [SetUp]
        public void SetUp()
        {
            TestSystem = World.GetOrCreateSystem<SystemBase_TestSystem>();
        }

        public partial class SystemBase_TestSystem : SystemBase
        {
            public int SingletonProperty
            {
                get => GetSingleton<EcsTestData>().value;
                set => SetSingleton(new EcsTestData {value = value});
            }

            public struct SingletonDataInSystem : IComponentData
            {
                public float value;
            }

            protected override void OnUpdate() {}

            public void SingletonPropertyGetterAndSetter()
            {
                EntityManager.CreateEntity(typeof(EcsTestData));

                var defaultSingletonValue = SingletonProperty;
                Assert.AreEqual(expected: 0, defaultSingletonValue);

                SingletonProperty = 10;
                Assert.AreEqual(10, GetSingleton<EcsTestData>().value);
            }

            public void GetSetSingleton()
            {
                EntityManager.CreateEntity(typeof(EcsTestData));

                SetSingleton(new EcsTestData(10));
                Assert.AreEqual(10, GetSingleton<EcsTestData>().value);
            }

            public void GetSetSingletonInSystem()
            {
                EntityManager.CreateEntity(typeof(SystemBase_TestSystem.SingletonDataInSystem));

                SetSingleton(new SystemBase_TestSystem.SingletonDataInSystem() { value = 10.0f });
                Assert.AreEqual(10, GetSingleton<SystemBase_TestSystem.SingletonDataInSystem>().value);
            }

            T GenericMethodWithSingletonAccess<T>(T value) where T : struct, IComponentData
            {
                SetSingleton(value);
                return GetSingleton<T>();
            }

            public void GetSetSingletonWithGenericParameter()
            {
                EntityManager.CreateEntity(typeof(EcsTestData));

                GenericMethodWithSingletonAccess(new EcsTestData(10));
                Assert.AreEqual(10, GetSingleton<EcsTestData>().value);
            }

            struct TestStruct { }

            int SingletonAccessInGenericMethodWithInParameter<T>(in T inArgument) where T: struct
            {
                return GetSingleton<EcsTestData>().value;
            }

            public void GetSingletonInGenericMethodWithInParameter()
            {
                var entity = EntityManager.CreateEntity(typeof(EcsTestData));
                EntityManager.SetComponentData(entity, new EcsTestData{ value = 10 });

                Assert.AreEqual(expected: 10, actual: SingletonAccessInGenericMethodWithInParameter(new TestStruct()));
            }

            public void SingletonMethodsWithValidFilter_GetsAndSets()
            {
                var queryWithFilter1 = EntityManager.CreateEntityQuery(typeof(EcsTestData), typeof(SharedData1));
                queryWithFilter1.SetSharedComponentFilter(new SharedData1(1));
                var queryWithFilter2 = EntityManager.CreateEntityQuery(typeof(EcsTestData), typeof(SharedData1));
                queryWithFilter2.SetSharedComponentFilter(new SharedData1(2));

                var entity1 = EntityManager.CreateEntity(typeof(EcsTestData), typeof(SharedData1));
                EntityManager.SetComponentData(entity1, new EcsTestData(-1));
                EntityManager.SetSharedComponentData(entity1, new SharedData1(1));

                var entity2 = EntityManager.CreateEntity(typeof(EcsTestData), typeof(SharedData1));
                EntityManager.SetComponentData(entity2, new EcsTestData(-1));
                EntityManager.SetSharedComponentData(entity2, new SharedData1(2));

                Assert.DoesNotThrow(() => queryWithFilter1.SetSingleton(new EcsTestData(1)));
                Assert.DoesNotThrow(() => queryWithFilter2.SetSingleton(new EcsTestData(2)));

                Assert.DoesNotThrow(() => queryWithFilter1.GetSingletonEntity());
                Assert.DoesNotThrow(() => queryWithFilter2.GetSingletonEntity());

                var data1 = queryWithFilter1.GetSingleton<EcsTestData>();
                Assert.AreEqual(1, data1.value);
                var data2 = queryWithFilter2.GetSingleton<EcsTestData>();
                Assert.AreEqual(2, data2.value);

                // These need to be reset or the AllSharedComponentReferencesAreFromChunks check will fail
                queryWithFilter1.ResetFilter();
                queryWithFilter2.ResetFilter();
            }

            public void SingletonMethodsWithInvalidFilter_Throws()
            {
                var queryWithFilterMissingEntity = EntityManager.CreateEntityQuery(typeof(EcsTestData), typeof(SharedData1));
                queryWithFilterMissingEntity.SetSharedComponentFilter(new SharedData1(1));
                var queryWithFilterWithAdditionalEntity = EntityManager.CreateEntityQuery(typeof(EcsTestData), typeof(SharedData1));
                queryWithFilterWithAdditionalEntity.SetSharedComponentFilter(new SharedData1(2));

                var entity1 = EntityManager.CreateEntity(typeof(EcsTestData), typeof(SharedData1));
                EntityManager.SetSharedComponentData(entity1, new SharedData1(2));
                var entity2 = EntityManager.CreateEntity(typeof(EcsTestData), typeof(SharedData1));
                EntityManager.SetSharedComponentData(entity2, new SharedData1(2));

                Assert.Throws<InvalidOperationException>(() => queryWithFilterMissingEntity.GetSingleton<EcsTestData>());
                Assert.Throws<InvalidOperationException>(() => queryWithFilterMissingEntity.SetSingleton(new EcsTestData(1)));
                Assert.Throws<InvalidOperationException>(() => queryWithFilterMissingEntity.GetSingletonEntity());

                Assert.Throws<InvalidOperationException>(() => queryWithFilterWithAdditionalEntity.GetSingleton<EcsTestData>());
                Assert.Throws<InvalidOperationException>(() => queryWithFilterWithAdditionalEntity.SetSingleton(new EcsTestData(1)));
                Assert.Throws<InvalidOperationException>(() => queryWithFilterWithAdditionalEntity.GetSingletonEntity());

                // These need to be reset or the AllSharedComponentReferencesAreFromChunks check will fail
                queryWithFilterMissingEntity.ResetFilter();
                queryWithFilterWithAdditionalEntity.ResetFilter();
            }

            public void GetSetSingletonMultipleComponents()
            {
                var entity = EntityManager.CreateEntity(typeof(EcsTestData3), typeof(EcsTestData), typeof(EcsTestData2));

                EntityManager.SetComponentData(entity, new EcsTestData(10));
                Assert.AreEqual(10, GetSingleton<EcsTestData>().value);

                SetSingleton(new EcsTestData2(100));
                Assert.AreEqual(100, EntityManager.GetComponentData<EcsTestData2>(entity).value0);
            }

            public void GetSetSingletonInEntitiesForEach()
            {
                EntityManager.CreateEntity(typeof(EcsTestData2));
                EntityManager.CreateEntity(typeof(EcsTestData));

                Entities.WithoutBurst().ForEach((in EcsTestData2 data2) => { SetSingleton(new EcsTestData(10)); }).Run();
                int value = 0;
                Entities.WithoutBurst().ForEach((in EcsTestData2 data2) => { value = GetSingleton<EcsTestData>().value; }).Run();

                Assert.AreEqual(10, value);
            }

            public void GetSetSingletonZeroThrows()
            {
                Assert.Throws<InvalidOperationException>(() => SetSingleton(new EcsTestData()));
                Assert.Throws<InvalidOperationException>(() => GetSingleton<EcsTestData>());
            }

            // Throws due to a singleton component only being allowed on a single Entity
            public void GetSetSingletonMultipleThrows()
            {
                EntityManager.CreateEntity(typeof(EcsTestData));
                EntityManager.CreateEntity(typeof(EcsTestData));

                Assert.Throws<InvalidOperationException>(() => SetSingleton(new EcsTestData()));
                Assert.Throws<InvalidOperationException>(() => GetSingleton<EcsTestData>());
            }

            public void RequireSingletonWorks()
            {
                RequireSingletonForUpdate<EcsTestData>();
                GetEntityQuery(typeof(EcsTestData2));

                EntityManager.CreateEntity(typeof(EcsTestData2));
                Assert.IsFalse(ShouldRunSystem());
                EntityManager.CreateEntity(typeof(EcsTestData));
                Assert.IsTrue(ShouldRunSystem());
            }

            public void HasSingletonWorks()
            {
                Assert.IsFalse(HasSingleton<EcsTestData>());
                EntityManager.CreateEntity(typeof(EcsTestData));
                Assert.IsTrue(HasSingleton<EcsTestData>());
            }

            public void HasSingleton_ReturnsTrueWithEntityWithOnlyComponent()
            {
                Assert.IsFalse(HasSingleton<EcsTestData>());

                EntityManager.CreateEntity(typeof(EcsTestData));
                Assert.IsTrue(HasSingleton<EcsTestData>());

                EntityManager.CreateEntity(typeof(EcsTestData));
                Assert.IsFalse(HasSingleton<EcsTestData>());
            }

            public void GetSingletonEntityWorks()
            {
                var entity = EntityManager.CreateEntity(typeof(EcsTestData));

                var singletonEntity = GetSingletonEntity<EcsTestData>();
                Assert.AreEqual(entity, singletonEntity);
            }

            public void GetSingletonThroughQueryWorks()
            {
                EntityQuery query = GetEntityQuery(ComponentType.ReadOnly<EcsTestData>(), ComponentType.ReadOnly<EcsTestData2>());
                RequireForUpdate(query);
                var entity = EntityManager.CreateEntity(typeof(EcsTestData), typeof(EcsTestData2));
                EntityManager.SetComponentData(entity, new EcsTestData() { value = 3 });

                Assert.AreEqual(3, query.GetSingleton<EcsTestData>().value);
            }

            public void SetSingletonWithExpressionWithPreprocessorTriviaWorks()
            {
                EntityManager.CreateEntity(typeof(EcsTestData));
                SetSingleton(new EcsTestData()
                {
#if SOME_DEFINE
                    value = 3
#endif
                });

                Assert.AreEqual(3, GetSingleton<EcsTestData>().value);
            }

            public struct GenericDataType<T> : IComponentData where T : unmanaged
            {
                public T value;
            }
            public void GetSingletonWithGenericThrows()
            {
                EntityManager.CreateEntity(typeof(GenericDataType<int>));

                SetSingleton(new GenericDataType<int>() { value = 10 });
                Assert.AreEqual(10, GetSingleton<GenericDataType<int>>().value);
            }

    #if !UNITY_DISABLE_MANAGED_COMPONENTS
            public void GetSetSingleton_ManagedComponents()
            {
                var entity = EntityManager.CreateEntity(typeof(EcsTestManagedComponent));

                const string kTestVal = "SomeString";
                this.SetSingleton(new EcsTestManagedComponent() { value = kTestVal });
                Assert.AreEqual(kTestVal, this.GetSingleton<EcsTestManagedComponent>().value);
            }

            public void HasSingletonWorks_ManagedComponents()
            {
                Assert.IsFalse(this.HasSingleton<EcsTestManagedComponent>());
                EntityManager.CreateEntity(typeof(EcsTestManagedComponent));
                Assert.IsTrue(this.HasSingleton<EcsTestManagedComponent>());
            }

    #endif
        }

        [Test]
        public void SystemBase_SingletonGetterAndSetter()
        {
            TestSystem.SingletonPropertyGetterAndSetter();
        }

        [Test]
        public void SystemBase_GetSetSingleton()
        {
            TestSystem.GetSetSingleton();
        }

        [Test]
        public void SystemBase_GetSetSingletonInSystem()
        {
            TestSystem.GetSetSingletonInSystem();
        }

        [Test]
        public void SystemBase_GetSetSingletonWithGenericParameter()
        {
            TestSystem.GetSetSingletonWithGenericParameter();
        }

        [Test]
        public void SystemBase_SingletonAccessInGenericMethodWithInParameter()
        {
            TestSystem.GetSingletonInGenericMethodWithInParameter();
        }

        [Test]
        public void SystemBase_SingletonMethodsWithValidFilter_GetsAndSets()
        {
            TestSystem.SingletonMethodsWithValidFilter_GetsAndSets();
        }

        [Test]
        public void SystemBase_SingletonMethodsWithInvalidFilter_Throws()
        {
            TestSystem.SingletonMethodsWithInvalidFilter_Throws();
        }

        [Test]
        public void SystemBase_GetSetSingletonMultipleComponents()
        {
            TestSystem.GetSetSingletonMultipleComponents();
        }

        [Test]
        public void SystemBase_GetSetSingletonInEntitiesForEach()
        {
            TestSystem.GetSetSingletonInEntitiesForEach();
        }

        [Test]
        public void SystemBase_GetSetSingletonZeroThrows()
        {
            TestSystem.GetSetSingletonZeroThrows();
        }

        [Test]
        public void SystemBase_GetSetSingletonMultipleThrows()
        {
            TestSystem.GetSetSingletonMultipleThrows();
        }

        [Test]
        public void SystemBase_RequireSingletonWorks()
        {
            TestSystem.RequireSingletonWorks();
        }

        [Test]
        public void SystemBase_HasSingletonWorks()
        {
            TestSystem.HasSingletonWorks();
        }

        [Test]
        public void SystemBase_HasSingleton_ReturnsTrueWithEntityWithOnlyComponent()
        {
            TestSystem.HasSingleton_ReturnsTrueWithEntityWithOnlyComponent();
        }

        [Test]
        public void SystemBase_GetSingletonEntityWorks()
        {
            TestSystem.GetSingletonEntityWorks();
        }

        [Test]
        public void SystemBase_GetSingletonThroughQueryWorks()
        {
            TestSystem.GetSingletonThroughQueryWorks();
        }

        [Test]
        public void SystemBase_SetSingletonWithExpressionWithPreprocessorTriviaWorks()
        {
            TestSystem.SetSingletonWithExpressionWithPreprocessorTriviaWorks();
        }

        [Test]
        public void SystemBase_GetSingletonWithGenericThrows()
        {
            // Will now throw exception around not knowing component types at compile time instead of crashing
            Assert.Throws<ArgumentException>(()=>TestSystem.GetSingletonWithGenericThrows());
        }

#if !UNITY_DISABLE_MANAGED_COMPONENTS
        [Test]
        public void SystemBase_GetSetSingleton_ManagedComponents()
        {
            TestSystem.GetSetSingleton_ManagedComponents();
        }

        [Test]
        public void SystemBase_HasSingletonWorks_ManagedComponents()
        {
            TestSystem.HasSingletonWorks_ManagedComponents();
        }

#endif
    }
}


