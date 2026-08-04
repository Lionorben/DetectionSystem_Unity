using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


//TO:DO: add single target mode toggle to sensor and detection manager
[RequireComponent(typeof(AISensor))]
public class DetectionManager : MonoBehaviour
{
    #region Sight Settings

    public float halfSightFOV = 90;

    public float sightDistance = 40;

    public float sightHeight = 20;


    public float suspicionThreshold = 10;

    public AnimationCurve inSightDetectionRate = AnimationCurve.Constant(0, 1, 1);

    public AnimationCurve outSightDetectionRate = AnimationCurve.Constant(0, 1, 1);

    public AnimationCurve distanceMult = AnimationCurve.Linear(0, 3, 1, 1);

    //[TabGroup("Performance Settings")]
    private int MaxSentientsAwareness = 5;

    public float frameTimeBetweenSightLoops = 30;

    public Dictionary<Sentient, float> monitoredSentients = new Dictionary<Sentient, float>();

    public List<Sentient> detectedSentients = new List<Sentient>();

    [SerializeField] private Color baseGizmoColor = new Color(1, 0, 0, 0.1f);
    [SerializeField] private Color detectedGizmoColor = new Color(0, 1, 0, 0.1f);

    public Action<Sentient> OnSentientDetected;

    #endregion

    public AISensor sensor;


    public void Start()
    {
        MaxSentientsAwareness = AISensorManager.Instance.MaxColliderHits;

        sensor = GetComponent<AISensor>();
        sensor.angle = halfSightFOV;
        sensor.distance = sightDistance;
        sensor.height = sightHeight;
        StartCoroutine(SightLogicLoop());
    }

    public IEnumerator SightLogicLoop()
    {
        while (true)
        {
            List<Sentient> sentientsInRadius = new List<Sentient>();
            foreach (GameObject obj in sensor.ObjectsInRadius)
            {
                Sentient currentSentient = obj.GetComponent<Sentient>();
                if (currentSentient == null)
                {
                    currentSentient = obj.GetComponentInParent<Sentient>();
                    if (currentSentient == null)
                    {
                        continue;
                    }
                }

                sentientsInRadius.Add(currentSentient);

                if (!monitoredSentients.ContainsKey(currentSentient))
                {
                    
                    AddSentientToMonitor(currentSentient);
                }
            }



            foreach (Sentient sentient in monitoredSentients.Keys.ToList())
            {
                if (!sentientsInRadius.Contains(sentient))
                {
                    RemoveSentitentFromMonitor(sentient);
                }
            }
        
            //update suspision
            UpdateSuspision();

            int frame = 0;
            while (frame < frameTimeBetweenSightLoops)
            {
                frame++;
                yield return null;
            }
        }
    }


    public void RemoveSentitentFromMonitor(Sentient sentitent)
    {
        if (monitoredSentients.ContainsKey(sentitent))
        {
            monitoredSentients.Remove(sentitent);
        }
        if (detectedSentients.Contains(sentitent))
        {
            detectedSentients.Remove(sentitent);
        }
    }

    public void AddSentientToMonitor(Sentient sentitent)
    {
        monitoredSentients.Add(sentitent, 0);
    }

    public void UpdateSuspision()
    {
        float highestSuspicion = 0;
        foreach (Sentient currentSentient in monitoredSentients.Keys.ToList())
        {
            float currentSuspicion = monitoredSentients[currentSentient];
            if (sensor.objectsInSight.Contains(currentSentient.collider.gameObject))
            {
                // Update suspicion based on in sight values
                currentSuspicion += GetInSightSuspicionChange(currentSentient);
            }
            else
            {
                // Update suspicion based on out of sight values
                currentSuspicion += GetOutOfSightSuspicionChange(currentSentient);
            }
            currentSuspicion = Mathf.Clamp(currentSuspicion, 0, suspicionThreshold);
            if (currentSuspicion >= suspicionThreshold)
            {
                //Debug.Log(currentSentient.name + " Detected by " + gameObject.name);
                SentientDetected(currentSentient);
            }

            // Update the dictionary directly
            monitoredSentients[currentSentient] = currentSuspicion;

            //check for highest suspicion change
            if(currentSuspicion > highestSuspicion) 
            {
                highestSuspicion = currentSuspicion;
            }
        }
        UpdateSensorColor(highestSuspicion);
    }

    private void UpdateSensorColor(float highestSuspicion) 
    {
        float normalizedSuspicion = highestSuspicion / suspicionThreshold;
        Color sensorColor = Color.Lerp(baseGizmoColor, detectedGizmoColor, 1 - normalizedSuspicion);
        sensor.UpdateMeshColor(sensorColor);
    }


    public float GetInSightSuspicionChange(Sentient sentient)
    {
        
        //get the normalized speed to use on the curves
        float normalizedSpeed = sentient.normalizedSpeed;
        //get inital detection rate
        float detectionRate = inSightDetectionRate.Evaluate(normalizedSpeed);
        //multiply the detection rate by sentient detectionMult
        detectionRate *= sentient.detectionMultiplier;

        //only do distance mult if detection rate is greater than 0
        if (detectionRate > 0)
        {
            //get the normalized distance between the sentient and us
            float normalizedDistance = Vector3.Distance(transform.position, sentient.transform.position) / sightDistance;
            //multiply the detection rate by our distance mult
            detectionRate *= distanceMult.Evaluate(normalizedDistance);
        }
        return detectionRate;
    }

    public float GetOutOfSightSuspicionChange(Sentient sentient)
    {
        //get the normalized speed to use on the curves
        float normalizedSpeed = sentient.normalizedSpeed;
        //get inital detection rate
        float detectionRate = outSightDetectionRate.Evaluate(normalizedSpeed);
        //multiply the detection rate by sentient detectionMult
        detectionRate *= sentient.detectionMultiplier;


        //only do distance mult if detection rate is greater than 0
        if(detectionRate > 0) 
        {
            //get the normalized distance between the sentient and us
            float normalizedDistance = Vector3.Distance(transform.position, sentient.transform.position) / sightDistance;
            //multiply the detection rate by our distance mult
            detectionRate *= distanceMult.Evaluate(normalizedDistance);
        }
        return detectionRate;
    }

    public void SentientDetected(Sentient sentient)
    {
        //Maximize 50 sentients for performance
        if (detectedSentients.Count < MaxSentientsAwareness)
        {
            if (!detectedSentients.Contains(sentient))
            {
                OnSentientDetected?.Invoke(sentient);
                detectedSentients.Add(sentient);
            }
        }
    }
}
