using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwitchScene : MonoBehaviour
{
    private static SwitchScene instance;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
            LoadScene(0);
        else if (Input.GetKeyDown(KeyCode.F2))
            LoadScene(1);
        else if (Input.GetKeyDown(KeyCode.F3))
            LoadScene(2);
        else if (Input.GetKeyDown(KeyCode.F4))
            LoadScene(3);
        else if (Input.GetKeyDown(KeyCode.F5))
            LoadScene(4);
        else if (Input.GetKeyDown(KeyCode.F6))
            LoadScene(5);
        else if (Input.GetKeyDown(KeyCode.F7))
            LoadScene(6);
        else if (Input.GetKeyDown(KeyCode.F8))
            LoadScene(7);
        else if (Input.GetKeyDown(KeyCode.F9))
            LoadScene(8);
        else if (Input.GetKeyDown(KeyCode.F10))
            LoadScene(9);

    }
    private void LoadScene(int sceneid)
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneid);
    }
}
