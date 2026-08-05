using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[RequireComponent(typeof(AISensor))]
public class DetectionManager : MonoBehaviour
{
    #region Sight Settings
    [SerializeField] private float halfSightFOV = 90f;
    [SerializeField] private float sightDistance = 40f;
    [SerializeField] private float sightHeight = 20f;


    [SerializeField] private float suspicionThreshold = 10f;

    [SerializeField] private AnimationCurve inSightDetectionRate = AnimationCurve.Constant(0, 1, 1);

    [SerializeField] private AnimationCurve outSightDetectionRate = AnimationCurve.Constant(0, 1, 1);

    [SerializeField] private AnimationCurve distanceMult = AnimationCurve.Linear(0, 3, 1, 1);

    private int _maxSentientsAwareness = 5;

    [SerializeField] private float timeBetweenSightLoops = 0.1f;

    private WaitForSeconds _sightLoopWaitForSeconds;

    private Dictionary<Sentient, float> _monitoredSentients = new Dictionary<Sentient, float>();

    private List<Sentient> _detectedSentients = new List<Sentient>();

    [SerializeField] private Color baseGizmoColor = new Color(1, 0, 0, 0.1f);
    [SerializeField] private Color detectedGizmoColor = new Color(0, 1, 0, 0.1f);

    public Action<Sentient> OnSentientDetected;

    #endregion

    private AISensor _sensor;


    public void Start()
    {
        _maxSentientsAwareness = AISensorManager.Instance.MaxColliderHits;
        _sightLoopWaitForSeconds = new WaitForSeconds(timeBetweenSightLoops);
        _sensor = GetComponent<AISensor>();
        _sensor.Angle = halfSightFOV;
        _sensor.Distance = sightDistance;
        _sensor.Height = sightHeight;
        StartCoroutine(SightLogicLoop());
    }

    /// <summary>
    /// Coroutine that continuously monitors all detected objects in radius on a set interval.
    /// Manages the addition and removal of sentients from the monitored list and triggers suspicion updates.
    /// </summary>
    public IEnumerator SightLogicLoop()
    {
        while (true)
        {
            List<Sentient> sentientsInRadius = new List<Sentient>();
            foreach (GameObject obj in _sensor.ObjectsInRadius)
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

                if (!_monitoredSentients.ContainsKey(currentSentient))
                {
                    
                    AddSentientToMonitor(currentSentient);
                }
            }

            foreach (Sentient sentient in _monitoredSentients.Keys.ToList())
            {
                if (!sentientsInRadius.Contains(sentient))
                {
                    RemoveSentitentFromMonitor(sentient);
                }
            }
        
            //update suspision
            UpdateSuspision();

            yield return _sightLoopWaitForSeconds; 
        }
    }


    /// <summary>
    /// Removes a sentient from being monitored and clears it from the detected list if present.
    /// </summary>
    public void RemoveSentitentFromMonitor(Sentient sentitent)
    {
        if (_monitoredSentients.ContainsKey(sentitent))
        {
            _monitoredSentients.Remove(sentitent);
        }
        if (_detectedSentients.Contains(sentitent))
        {
            _detectedSentients.Remove(sentitent);
        }
    }

    /// <summary>
    /// Adds a newly encountered sentient to the monitoring dictionary with an initial suspicion of zero.
    /// </summary>
    public void AddSentientToMonitor(Sentient sentitent)
    {
        _monitoredSentients.Add(sentitent, 0);
    }

    /// <summary>
    /// Evaluates the suspicion level for all monitored sentients. 
    /// Suspicion increases differently depending on whether the sentient is currently in line of sight or not.
    /// Triggers detection if suspicion exceeds the designated threshold.
    /// </summary>
    public void UpdateSuspision()
    {
        float highestSuspicion = 0;
        foreach (Sentient currentSentient in _monitoredSentients.Keys.ToList())
        {
            float currentSuspicion = _monitoredSentients[currentSentient];
            if (_sensor.ObjectsInSight.Contains(currentSentient.SentientCollider.gameObject))
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
                SentientDetected(currentSentient);
            }

            // Update the dictionary directly
            _monitoredSentients[currentSentient] = currentSuspicion;

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
        _sensor.UpdateMeshColor(sensorColor);
    }


    /// <summary>
    /// Calculates the change in suspicion for a sentient that is currently within the sensor's line of sight.
    /// Factors in the sentient's normalized speed, individual detection multiplier, and distance from the sensor.
    /// </summary>
    public float GetInSightSuspicionChange(Sentient sentient)
    {
        
        //get the normalized speed to use on the curves
        float normalizedSpeed = sentient.NormalizedSpeed;
        //get inital detection rate
        float detectionRate = inSightDetectionRate.Evaluate(normalizedSpeed);
        //multiply the detection rate by sentient detectionMult
        detectionRate *= sentient.DetectionMultiplier;

        //only do distance mult if detection rate is greater than 0
        if (detectionRate > 0)
        {
            //get the normalized distance between the sentient and us
            float normalizedDistance = Vector3.Distance(transform.position, sentient.transform.position) / sightDistance;
            //multiply the detection rate by our distance mult
            detectionRate *= distanceMult.Evaluate(normalizedDistance);
        }
        return detectionRate * timeBetweenSightLoops;
    }

    /// <summary>
    /// Calculates the change in suspicion for a sentient that is currently outside the sensor's line of sight,
    /// usually resulting in a slower suspicion gain or decay based on the out-of-sight animation curve.
    /// </summary>
    public float GetOutOfSightSuspicionChange(Sentient sentient)
    {
        //get the normalized speed to use on the curves
        float normalizedSpeed = sentient.NormalizedSpeed;
        //get inital detection rate
        float detectionRate = outSightDetectionRate.Evaluate(normalizedSpeed);
        //multiply the detection rate by sentient detectionMult
        detectionRate *= sentient.DetectionMultiplier;


        //only do distance mult if detection rate is greater than 0
        if(detectionRate > 0) 
        {
            //get the normalized distance between the sentient and us
            float normalizedDistance = Vector3.Distance(transform.position, sentient.transform.position) / sightDistance;
            //multiply the detection rate by our distance mult
            detectionRate *= distanceMult.Evaluate(normalizedDistance);
        }
        return detectionRate * timeBetweenSightLoops;
    }

    /// <summary>
    /// Registers a sentient as fully detected, adding it to the awareness list and firing the detection event.
    /// Ensures we do not exceed the maximum allowed number of simultaneously detected sentients.
    /// </summary>
    public void SentientDetected(Sentient sentient)
    {
        if (_detectedSentients.Count < _maxSentientsAwareness)
        {
            if (!_detectedSentients.Contains(sentient))
            {
                OnSentientDetected?.Invoke(sentient);
                _detectedSentients.Add(sentient);
            }
        }
    }
}
