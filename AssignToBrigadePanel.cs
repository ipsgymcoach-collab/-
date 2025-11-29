using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class AssignToBrigadePanel : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Transform listContainer;
    [SerializeField] private GameObject rowPrefab;
    [SerializeField] private Button laterButton;
    [SerializeField] private Button cancelButton;

    private WorkerData pendingWorker;
    private System.Action<BrigadeData> onAssign;
    private System.Action onHireLater;
    private System.Action onCancel;

    private void Awake()
    {
        if (cancelButton != null)
            cancelButton.onClick.AddListener(CloseWithoutHire);

        if (laterButton != null)
            laterButton.onClick.AddListener(HireWithoutAssignment);

        if (root != null)
            root.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    public void Open(WorkerData worker, System.Action<BrigadeData> assignCallback, System.Action hireLaterCallback)
    {
        pendingWorker = worker;
        onAssign = assignCallback;
        onHireLater = hireLaterCallback;

        if (root != null) root.SetActive(true);
        else gameObject.SetActive(true);

        StartCoroutine(ShowNextFrame());
    }

    private IEnumerator ShowNextFrame()
    {
        yield return null;
        BuildList();
    }

    private void BuildList()
    {
        ClearList();

        var data = GameManager.Instance.CurrentGame;
        if (data == null)
        {
            Debug.LogError("[AssignToBrigadePanel] ❌ Нет GameData");
            CloseWithoutHire();
            return;
        }

        var brigades = data.allBrigades ?? new List<BrigadeData>();
        if (brigades.Count == 0)
        {
            HUDController.Instance?.ShowToast("⚠ Нет доступных бригад!");
            return;
        }

        foreach (var b in brigades)
        {
            if (b == null) continue;

            var go = Instantiate(rowPrefab, listContainer);
            var btn = go.GetComponentInChildren<Button>(true);
            var text = go.GetComponentInChildren<TMP_Text>(true);

            string display = b.name;
            var foreman = data.foremen.Find(f => f.id == b.foremanId);
            if (foreman != null)
                display = $"{b.name} ({foreman.name})";

            if (text != null) text.text = display;

            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    if (b.isWorking)
                    {
                        HUDController.Instance?.ShowToast("❌ Нельзя назначить работника в активную бригаду!");
                        return;
                    }

                    onAssign?.Invoke(b);
                    Close();
                });
            }
        }
    }

    private void ClearList()
    {
        if (listContainer == null) return;
        for (int i = listContainer.childCount - 1; i >= 0; i--)
            Destroy(listContainer.GetChild(i).gameObject);
    }

    private void HireWithoutAssignment()
    {
        onHireLater?.Invoke();
        Close();
    }

    private void CloseWithoutHire()
    {
        onCancel?.Invoke();
        Close();
    }

    public void Close()
    {
        if (root != null) root.SetActive(false);
        else gameObject.SetActive(false);

        pendingWorker = null;
        onAssign = null;
        onHireLater = null;
        onCancel = null;
        ClearList();
    }
}
