// using Unity.Collections;
// using Unity.Entities;
// using UnityEngine;
//
// namespace Finite_State_Machine
// {
//     public partial struct StateChangeSystem : ISystem
//     {
//         public void OnCreate(ref SystemState state)
//         {
//             state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<FSMStateData, FSMStateChangeSignal>().Build());
//         }
//         public void OnUpdate(ref SystemState state)
//         {
//             EntityCommandBuffer.ParallelWriter ecb = new EntityCommandBuffer(Allocator.TempJob).AsParallelWriter();
//             new StateChangeJob
//             {
//                 ecb = ecb,
//             }.ScheduleParallel(state.Dependency);
//         }
//
//         [WithAll(typeof(FSMStateData))]
//         [WithAll(typeof(FSMStateChangeSignal))]
//         partial struct StateChangeJob : IJobEntity
//         {
//             public EntityCommandBuffer.ParallelWriter ecb;
//             void Execute(Entity entity, [ChunkIndexInQuery] int chunkIndexInQuery, ref FSMStateData stateData, in FSMStateChangeSignal signal)
//             {
//                 // TODO : State change logic
//                 stateData.prevState = stateData.currentState;
//                 stateData.currentState = signal.nextState;
//                 
//                 ecb.RemoveComponent<FSMStateChangeSignal>(chunkIndexInQuery, entity);
//             }
//         }
//     }
// }