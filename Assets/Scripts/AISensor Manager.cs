using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class AISensorManager : MonoBehaviour
{
    public static AISensorManager Instance;
    private void Awake()
    {
        if (!Instance)
        {
            Instance = this;
        }
    }

    //Maximum of collider hits per animal
    public int MaxColliderHits = 5;

    //Non native lists
    private List<OverlapSphereCommand> commandList;
    private ColliderHit[] resultArrayCopy;

    //Native lists
    private NativeList<OverlapSphereCommand> overlapSphereCommands;
    private NativeList<ColliderHit> overlapSphereResults;
    //Don't set this too low because it needs to resize everytime there is 1 new overlapSphere more than the allocated capacity, which is slow and can cause issues
    private int startingMemoryAlloc = 10000;

    //Cached Parameters
    private QueryParameters queryParameters;
    private ColliderHit cachedColliderHit = new ColliderHit();

    //Job
    [NonSerialized]
    public JobHandle overlapSphereJobHandle;
    private bool runOverlapSphereJob = true;

    //Memory allocation
    private void OnEnable()
    {
        queryParameters.hitTriggers = QueryTriggerInteraction.Collide;

        overlapSphereCommands = new NativeList<OverlapSphereCommand>(startingMemoryAlloc, Allocator.Persistent);
        overlapSphereResults = new NativeList<ColliderHit>(startingMemoryAlloc * MaxColliderHits, Allocator.Persistent);

        commandList = new List<OverlapSphereCommand>();
        resultArrayCopy = new ColliderHit[0];
    }

    //Clean it all up
    private void OnDisable()
    {
        try
        {
            overlapSphereCommands.Dispose();
            overlapSphereResults.Dispose();
        }
        catch { }
    }

    public int AddOverlapSphere(Vector3 pos, float distance, LayerMask layers, int id = -1)
    {
        queryParameters.layerMask = layers;

        //Add new animals overlapSphere
        if (id == -1)
        {
            //Instantiate(GameObject.CreatePrimitive(PrimitiveType.Sphere), pos, Quaternion.identity);
            commandList.Add(new OverlapSphereCommand(pos, distance, queryParameters));

            id = commandList.Count - 1;
        }
        //Update current animals overlapSphere 
        else
        {
            //Instantiate(GameObject.CreatePrimitive(PrimitiveType.Cube), pos, Quaternion.identity);
            commandList[id] = new OverlapSphereCommand(pos, distance, queryParameters);
        }

        return id;
    }

    //remove the command, because the animal is destroyed (a bit hacky, we don't actually remove the command, we just make it really, really cheap)
    public void RemoveOverlapSphere(int id)
    {
        var command = commandList[id];
        command.point = Vector3.zero;
        command.radius = 0;
        commandList[id] = command;
    }

    //Return the collider results
    public Collider[] GetOverlapSphereResults(int id)
    {
        try
        {
            Collider[] colliders = new Collider[MaxColliderHits];
            int index = id * MaxColliderHits;
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i] = resultArrayCopy[index + i].collider;
            }
            return colliders;
        }
        catch
        {
            return null;
        }
    }

    private void Update()
    {
        if (commandList.Count > 0)
        {
            //Run Job
            if (runOverlapSphereJob)
            {
                overlapSphereCommands.Clear();
                overlapSphereResults.Clear();

                for (int i = 0; i < commandList.Count; i++)
                {
                    overlapSphereCommands.Add(commandList[i]);

                    for (int j = 0; j < MaxColliderHits; j++)
                    {
                        overlapSphereResults.Add(cachedColliderHit);
                    }
                }

                overlapSphereJobHandle = OverlapSphereCommand.ScheduleBatch(overlapSphereCommands, overlapSphereResults, 1, MaxColliderHits);
            }
            //Get Results and copy them to a readable array
            else
            {
                overlapSphereJobHandle.Complete();

                if (overlapSphereResults.Length != resultArrayCopy.Length)
                {
                    resultArrayCopy = new ColliderHit[overlapSphereResults.Length];
                }

                NativeArray<ColliderHit>.Copy(overlapSphereResults, resultArrayCopy, overlapSphereResults.Length);
            }
            runOverlapSphereJob = !runOverlapSphereJob;
        }
    }
}
