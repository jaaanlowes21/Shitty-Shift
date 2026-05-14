using UnityEngine;
using UnityEngine.Events;

public class ElectricalPowerPuzzle : MonoBehaviour
{
    public enum ElectricalState
    {
        Inactive,
        SearchingForFuel,
        CarryingFuel,
        GeneratorFueled,
        GeneratorRunning,
        BreakerSolved,
        PowerRestored
    }

    [Header("Progress")]
    public ElectricalState currentState = ElectricalState.Inactive;
    public int correctBreakerButtonIndex = 0;
    public int selectedBreakerButtonIndex = -1;

    [Header("Breaker")]
    public BreakerPanelRaycast breakerPanel;

    [Header("Power Targets")]
    public Light[] controlledLights;
    public Behaviour[] flickerControllers;
    public float blackoutIntensity = 0f;

    [Header("Scene Objects")]
    [Tooltip("Objects enabled when the electrical puzzle starts, such as fuel, generator markers, breaker panel, or lever.")]
    public GameObject[] enableWhenPuzzleStarts;

    [Tooltip("Objects enabled after power is restored, such as the next minigame trigger.")]
    public GameObject[] enableWhenPowerRestored;

    [Tooltip("Objects disabled after power is restored, such as blockers or temporary puzzle prompts.")]
    public GameObject[] disableWhenPowerRestored;

    [Header("Messages")]
    public string blackoutMessage = "The lights went out. I need to restore the electricity.";
    public string needFuelMessage = "The generator is empty. I need to find fuel.";
    public string fuelPickupMessage = "Fuel tank picked up. I should bring this to the generator.";
    public string generatorFueledMessage = "The generator has fuel now. I should start it.";
    public string generatorStartedMessage = "The generator is running. Now I need to fix the breaker.";
    public string breakerSelectedMessage = "Switch selected. I should try the lever.";
    public string breakerWrongMessage = "Wrong switch. It reset. I need to try another one.";
    public string breakerCorrectMessage = "That was the right switch.";
    public string leverLockedMessage = "The lever will not work yet.";
    public string leverNeedsBreakerMessage = "I need to choose one breaker switch first.";
    public string powerRestoredMessage = "The electricity is back. I can move on.";

    [Header("Objective Guide")]
    public bool updateObjectiveGuide = true;
    public string findGeneratorObjective = "Find the generator.";
    public string findFuelObjective = "Find fuel for the generator.";
    public string fuelGeneratorObjective = "Bring the fuel to the generator.";
    public string startGeneratorObjective = "Start the generator.";
    public string chooseBreakerObjective = "Flip one breaker switch.";
    public string testLeverObjective = "Test the switch with the lever.";
    public string restoredPowerObjective = "Use the AVR remote again.";

    [Header("Events")]
    public UnityEvent onPuzzleStarted;
    public UnityEvent onPowerRestored;

    private float[] originalLightIntensities;
    private bool[] originalFlickerEnabled;

    public bool IsActive => currentState != ElectricalState.Inactive && currentState != ElectricalState.PowerRestored;
    public bool HasFuel => currentState >= ElectricalState.GeneratorFueled;
    public bool IsCarryingFuel => currentState == ElectricalState.CarryingFuel;
    public bool IsGeneratorRunning => currentState >= ElectricalState.GeneratorRunning;
    public bool IsBreakerSolved => currentState >= ElectricalState.BreakerSolved;
    public bool IsPowerRestored => currentState == ElectricalState.PowerRestored;
    public bool HasSelectedBreakerButton => selectedBreakerButtonIndex >= 0;

    private void Awake()
    {
        CachePowerTargets();
    }

    private void Start()
    {
        SetObjectsActive(enableWhenPowerRestored, false);
    }

    public void BeginPuzzle()
    {
        if (currentState != ElectricalState.Inactive)
            return;

        currentState = ElectricalState.SearchingForFuel;
        selectedBreakerButtonIndex = -1;
        TurnPowerOff();
        SetObjectsActive(enableWhenPuzzleStarts, true);
        ShowHint(blackoutMessage);
        SetObjective(findGeneratorObjective);
        onPuzzleStarted?.Invoke();
    }

    public void InspectGenerator()
    {
        if (!IsActive)
            return;

        if (currentState == ElectricalState.SearchingForFuel)
        {
            ShowHint(needFuelMessage);
            SetObjective(findFuelObjective);
            return;
        }

        if (currentState == ElectricalState.CarryingFuel)
        {
            FillGenerator();
            return;
        }

        if (currentState == ElectricalState.GeneratorFueled)
        {
            ShowHint(generatorFueledMessage);
            return;
        }

        if (currentState == ElectricalState.GeneratorRunning)
        {
            ShowHint(generatorStartedMessage);
            return;
        }

        if (currentState == ElectricalState.BreakerSolved)
            ShowHint(breakerCorrectMessage);
    }

    public bool PickupFuel()
    {
        if (currentState != ElectricalState.SearchingForFuel)
        {
            if (currentState == ElectricalState.Inactive)
                ShowHint("I do not need this yet.");

            return false;
        }

        currentState = ElectricalState.CarryingFuel;
        ShowHint(fuelPickupMessage);
        SetObjective(fuelGeneratorObjective);
        return true;
    }

    public bool FillGenerator()
    {
        if (currentState != ElectricalState.CarryingFuel)
        {
            InspectGenerator();
            return false;
        }

        currentState = ElectricalState.GeneratorFueled;
        ShowHint(generatorFueledMessage);
        SetObjective(startGeneratorObjective);
        return true;
    }

    public bool StartGenerator()
    {
        if (currentState == ElectricalState.GeneratorFueled)
        {
            currentState = ElectricalState.GeneratorRunning;
            ShowHint(generatorStartedMessage);
            SetObjective(chooseBreakerObjective);
            return true;
        }

        if (currentState == ElectricalState.SearchingForFuel || currentState == ElectricalState.CarryingFuel)
        {
            ShowHint(needFuelMessage);
            return false;
        }

        if (currentState == ElectricalState.GeneratorRunning || currentState == ElectricalState.BreakerSolved)
            ShowHint(generatorStartedMessage);

        return false;
    }

    public bool PressBreakerButton(int buttonIndex)
    {
        if (currentState != ElectricalState.GeneratorRunning)
        {
            if (currentState < ElectricalState.GeneratorRunning)
                ShowHint("The breaker has no power. I need the generator running first.");

            return false;
        }

        selectedBreakerButtonIndex = buttonIndex;
        ShowHint(breakerSelectedMessage);
        SetObjective(testLeverObjective);
        return true;
    }

    public bool PullLever()
    {
        if (currentState < ElectricalState.GeneratorRunning)
        {
            ShowHint(leverLockedMessage);
            return false;
        }

        if (currentState == ElectricalState.GeneratorRunning && !HasSelectedBreakerButton)
        {
            ShowHint(leverNeedsBreakerMessage);
            return false;
        }

        if (currentState == ElectricalState.GeneratorRunning && selectedBreakerButtonIndex != correctBreakerButtonIndex)
        {
            selectedBreakerButtonIndex = -1;

            if (breakerPanel != null)
                breakerPanel.ResetSelectedSwitch();

            ShowHint(breakerWrongMessage);
            SetObjective(chooseBreakerObjective);
            return false;
        }

        currentState = ElectricalState.BreakerSolved;
        ShowHint(breakerCorrectMessage);

        currentState = ElectricalState.PowerRestored;
        TurnPowerOn();
        SetObjectsActive(enableWhenPowerRestored, true);
        SetObjectsActive(disableWhenPowerRestored, false);
        ShowHint(powerRestoredMessage);
        SetObjective(restoredPowerObjective);
        onPowerRestored?.Invoke();
        return true;
    }

    private void CachePowerTargets()
    {
        if (controlledLights == null)
            controlledLights = new Light[0];

        originalLightIntensities = new float[controlledLights.Length];
        for (int i = 0; i < controlledLights.Length; i++)
        {
            if (controlledLights[i] != null)
                originalLightIntensities[i] = controlledLights[i].intensity;
        }

        if (flickerControllers == null)
            flickerControllers = new Behaviour[0];

        originalFlickerEnabled = new bool[flickerControllers.Length];
        for (int i = 0; i < flickerControllers.Length; i++)
        {
            if (flickerControllers[i] != null)
                originalFlickerEnabled[i] = flickerControllers[i].enabled;
        }
    }

    private void TurnPowerOff()
    {
        for (int i = 0; i < flickerControllers.Length; i++)
        {
            if (flickerControllers[i] != null)
                flickerControllers[i].enabled = false;
        }

        for (int i = 0; i < controlledLights.Length; i++)
        {
            if (controlledLights[i] != null)
                controlledLights[i].intensity = blackoutIntensity;
        }
    }

    private void TurnPowerOn()
    {
        for (int i = 0; i < controlledLights.Length; i++)
        {
            if (controlledLights[i] != null && i < originalLightIntensities.Length)
                controlledLights[i].intensity = originalLightIntensities[i];
        }

        for (int i = 0; i < flickerControllers.Length; i++)
        {
            if (flickerControllers[i] != null && i < originalFlickerEnabled.Length)
                flickerControllers[i].enabled = originalFlickerEnabled[i];
        }
    }

    private void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        foreach (GameObject obj in objects)
        {
            if (obj != null)
                obj.SetActive(active);
        }
    }

    private void ShowHint(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
            HintManager.Instance?.ShowHint(message);
    }

    private void SetObjective(string objective)
    {
        if (updateObjectiveGuide && !string.IsNullOrWhiteSpace(objective))
            ObjectiveManager.Instance?.SetObjective(objective);
    }
}
