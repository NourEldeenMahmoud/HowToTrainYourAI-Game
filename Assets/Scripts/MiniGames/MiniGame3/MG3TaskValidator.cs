using System.Collections.Generic;
using UnityEngine;

public class MG3TaskValidator : MonoBehaviour
{
    [SerializeField] private bool verboseLogs = true;

    public bool ValidateTask(MG3TaskDefinition task)
    {
        if (task == null || task.Slots == null || task.Slots.Length == 0)
        {
            return false;
        }

        // Build device map only from this task's own devices so that devices
        // belonging to other tasks cannot light up or satisfy this task's slots.
        MG3PushableDevice[] taskDevices = task.Devices;
        var devicesByCoord = new Dictionary<Vector2Int, MG3PushableDevice>(taskDevices != null ? taskDevices.Length : 0);
        if (taskDevices != null)
        {
            for (int i = 0; i < taskDevices.Length; i++)
            {
                MG3PushableDevice d = taskDevices[i];
                if (d != null)
                {
                    devicesByCoord[d.CurrentCoordinate] = d;
                }
            }
        }

        bool allSolved = true;
        switch (task.TaskType)
        {
            case MG3TaskType.ExactPlacement:
                allSolved = ValidateExact(task.Slots, devicesByCoord);
                break;
            case MG3TaskType.GroupPlacement:
                allSolved = ValidateGroup(task.Slots, devicesByCoord);
                break;
            case MG3TaskType.SizeOrdering:
                allSolved = ValidateSizeOrdering(task.Slots, devicesByCoord);
                break;
        }

        if (verboseLogs)
        {
            Debug.Log($"[MG3TaskValidator] Task '{task.TaskName}' ({task.TaskType}) solved={allSolved}", this);
        }

        return allSolved;
    }

    public void LockSolvedDevices(MG3TaskDefinition task)
    {
        if (task == null || task.Slots == null)
        {
            return;
        }

        // Scope to task devices only — same as ValidateTask.
        MG3PushableDevice[] taskDevices = task.Devices;
        var devicesByCoord = new Dictionary<Vector2Int, MG3PushableDevice>(taskDevices != null ? taskDevices.Length : 0);
        if (taskDevices != null)
        {
            for (int i = 0; i < taskDevices.Length; i++)
            {
                MG3PushableDevice d = taskDevices[i];
                if (d != null)
                {
                    devicesByCoord[d.CurrentCoordinate] = d;
                }
            }
        }

        for (int i = 0; i < task.Slots.Length; i++)
        {
            MG3TargetSlot slot = task.Slots[i];
            if (slot == null || !slot.IsSolved)
            {
                continue;
            }

            if (devicesByCoord.TryGetValue(slot.Coordinate, out MG3PushableDevice device) && device != null)
            {
                device.SetLocked(true);
            }
        }
    }

    private static bool ValidateExact(MG3TargetSlot[] slots, Dictionary<Vector2Int, MG3PushableDevice> devicesByCoord)
    {
        bool allSolved = true;
        for (int i = 0; i < slots.Length; i++)
        {
            MG3TargetSlot slot = slots[i];
            bool solved = false;

            if (slot != null && devicesByCoord.TryGetValue(slot.Coordinate, out MG3PushableDevice device) && device != null)
            {
                solved = !string.IsNullOrEmpty(slot.RequiredDeviceId) && device.DeviceId == slot.RequiredDeviceId;
            }

            if (slot != null)
            {
                slot.SetSolved(solved);
            }

            allSolved &= solved;
        }

        return allSolved;
    }

    private static bool ValidateGroup(MG3TargetSlot[] slots, Dictionary<Vector2Int, MG3PushableDevice> devicesByCoord)
    {
        bool allSolved = true;
        for (int i = 0; i < slots.Length; i++)
        {
            MG3TargetSlot slot = slots[i];
            bool solved = false;

            if (slot != null && devicesByCoord.TryGetValue(slot.Coordinate, out MG3PushableDevice device) && device != null)
            {
                solved = !string.IsNullOrEmpty(slot.RequiredGroupId) && device.GroupId == slot.RequiredGroupId;
            }

            if (slot != null)
            {
                slot.SetSolved(solved);
            }

            allSolved &= solved;
        }

        return allSolved;
    }

    private static bool ValidateSizeOrdering(MG3TargetSlot[] slots, Dictionary<Vector2Int, MG3PushableDevice> devicesByCoord)
    {
        bool allSolved = true;
        for (int i = 0; i < slots.Length; i++)
        {
            MG3TargetSlot slot = slots[i];
            bool solved = false;

            if (slot != null && devicesByCoord.TryGetValue(slot.Coordinate, out MG3PushableDevice device) && device != null)
            {
                solved = device.SizeRank == slot.RequiredSizeRank;
            }

            if (slot != null)
            {
                slot.SetSolved(solved);
            }

            allSolved &= solved;
        }

        return allSolved;
    }
}
