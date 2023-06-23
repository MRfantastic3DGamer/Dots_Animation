using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

namespace Refference
{
    public struct MirroredPlayerTransform : IComponentData
    {
        public float3 Position;
    }
    
    public class PlayerTransformReference : MonoBehaviour
    {
        [SerializeField] Mesh _mesh;
        [SerializeField] Material _material;
        EntityManager _entityManager;
        World _world;
        Entity _entity;
        void OnEnable ()
        {
            _world = World.DefaultGameObjectInjectionWorld;
            _entityManager = _world.EntityManager;
         
            _entity = _entityManager.CreateEntity();
            _entityManager.AddComponent<MirroredPlayerTransform>( _entity );
            _entityManager.AddComponentObject( _entity , this );// optional
            _entityManager.AddComponent<LocalToWorld>( _entity );
            RenderMeshUtility.AddComponents(
                _entity , _entityManager ,
                new RenderMeshDescription( UnityEngine.Rendering.ShadowCastingMode.On , receiveShadows:true , renderingLayerMask:1 ) ,
                new RenderMeshArray( new Material[]{ _material } , new Mesh[]{ _mesh } ) ,
                MaterialMeshInfo.FromRenderMeshArrayIndices( 0 , 0 )
            );
 
#if UNITY_EDITOR
            _entityManager.SetName( _entity , $"{gameObject.name} #{gameObject.GetInstanceID()}" );
#endif
        }
        
        void Update ()
        {
            float3 pos = transform.position;
            _entityManager.SetComponentData( _entity , new LocalToWorld{
                Value = transform.localToWorldMatrix
            } );
            _entityManager.SetComponentData(_entity, new MirroredPlayerTransform
            {
                Position = pos,
            });
        }
        void OnDisable ()
        {
            if(_world.IsCreated)
                _entityManager.DestroyEntity( _entity );
        }
        
    }
}