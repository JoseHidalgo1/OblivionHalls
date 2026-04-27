using System;
using UnityEngine;

public class PlayerFoodEnergy : MonoBehaviour
{
    [Header("Comida")]
    [SerializeField] private int maxFood = 10;
    [SerializeField] private float distancePerStep = 0.4f;
    [SerializeField] private int stepsPerFood = 10;
    [SerializeField] private float foodParticleDuration = 0.2f;
    [SerializeField] private float foodSlowdownAmount = 0.2f;

    [Header("Energia")]
    [SerializeField] private int maxEnergy = 5;
    [SerializeField] private float energyDrainPerSecond = 1f;
    [SerializeField] private float energyRechargePerSecond = 1f;
    [SerializeField] private float runMultiplier = 2f;
    [SerializeField] private float energyParticleDuration = 0.2f;

    public event Action<int, int, bool> OnFoodChanged;
    public event Action<int, int, bool> OnEnergyChanged;

    public int CurrentFood => currentFood;
    public int MaxFood => maxFood;
    public int CurrentEnergy => currentEnergy;
    public int MaxEnergy => maxEnergy;
    public bool IsRunning => isRunning;
    public bool IsFoodParticleActive => foodParticleTimer > 0f;
    public bool IsEnergyParticleActive => energyParticleTimer > 0f;

    private int currentFood;
    private int currentEnergy;
    private float walkedDistance;
    private int stepCounter;
    private float foodParticleTimer;
    private float energyParticleTimer;
    private float energyDrainAccumulator;
    private float energyRechargeAccumulator;
    private bool isRunning;

    void Awake()
    {
        currentFood = Mathf.Max(0, maxFood);
        currentEnergy = Mathf.Clamp(maxEnergy, 0, maxEnergy);
    }

    void Start()
    {
        NotifyFoodChanged(false);
        NotifyEnergyChanged(false);
    }

    void Update()
    {
        bool foodTimerActive = foodParticleTimer > 0f;
        bool energyTimerActive = energyParticleTimer > 0f;

        if (foodTimerActive)
        {
            foodParticleTimer -= Time.deltaTime;
            if (foodParticleTimer <= 0f)
            {
                NotifyFoodChanged(false);
            }
        }

        if (energyTimerActive)
        {
            energyParticleTimer -= Time.deltaTime;
            if (energyParticleTimer <= 0f)
            {
                NotifyEnergyChanged(false);
            }
        }
    }

    public float GetEffectiveSpeed(float baseSpeed, Vector2 movementInput)
    {
        float speed = baseSpeed;
        if (currentFood <= 0)
        {
            speed = Mathf.Max(0f, speed - foodSlowdownAmount);
        }

        if (isRunning && currentEnergy > 0)
        {
            speed *= runMultiplier;
        }

        return speed;
    }

    public void RestoreFood(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        int previousFood = currentFood;
        currentFood = Mathf.Min(maxFood, currentFood + amount);
        if (currentFood != previousFood)
        {
            foodParticleTimer = foodParticleDuration;
            NotifyFoodChanged(true);
        }
    }

    public void RegisterMovement(Vector2 movementInput, float actualSpeed, float deltaTime, bool shiftHeld)
    {
        bool isMoving = movementInput.sqrMagnitude > 0.0001f;
        if (isMoving)
        {
            walkedDistance += actualSpeed * deltaTime;
            while (walkedDistance >= distancePerStep)
            {
                walkedDistance -= distancePerStep;
                stepCounter++;
                if (stepCounter >= stepsPerFood)
                {
                    stepCounter = 0;
                    ConsumeFood();
                }
            }
        }

        UpdateEnergy(shiftHeld && isMoving, deltaTime);
    }

    private void ConsumeFood()
    {
        if (currentFood <= 0)
        {
            return;
        }

        currentFood = Mathf.Max(0, currentFood - 1);
        foodParticleTimer = foodParticleDuration;
        NotifyFoodChanged(true);
    }

    private void UpdateEnergy(bool wantsToRun, float deltaTime)
    {
        if (wantsToRun && currentEnergy > 0)
        {
            isRunning = true;
            energyRechargeAccumulator = 0f;
            energyParticleTimer = energyParticleDuration;
            energyDrainAccumulator += energyDrainPerSecond * deltaTime;

            int fullDrain = Mathf.FloorToInt(energyDrainAccumulator);
            if (fullDrain > 0)
            {
                energyDrainAccumulator -= fullDrain;
                int previousEnergy = currentEnergy;
                currentEnergy = Mathf.Max(0, currentEnergy - fullDrain);
                energyParticleTimer = energyParticleDuration;
                NotifyEnergyChanged(true);

                if (currentEnergy <= 0 && previousEnergy > 0)
                {
                    isRunning = false;
                }
            }
        }
        else
        {
            if (isRunning)
            {
                isRunning = false;
            }

            energyDrainAccumulator = 0f;
            energyRechargeAccumulator += energyRechargePerSecond * deltaTime;
            int fullRecharge = Mathf.FloorToInt(energyRechargeAccumulator);
            if (fullRecharge > 0)
            {
                energyRechargeAccumulator -= fullRecharge;
                int previousEnergy = currentEnergy;
                currentEnergy = Mathf.Min(maxEnergy, currentEnergy + fullRecharge);
                if (currentEnergy != previousEnergy)
                {
                    NotifyEnergyChanged(false);
                }
            }
        }
    }

    private void NotifyFoodChanged(bool useParticles)
    {
        OnFoodChanged?.Invoke(currentFood, maxFood, useParticles);
    }

    private void NotifyEnergyChanged(bool useParticles)
    {
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy, useParticles);
    }
}
