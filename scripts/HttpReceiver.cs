using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class HttpReceiver : MonoBehaviour
{
    public bool serverOn;

    // Start is called before the first frame update
    void Start()
    {
        if(serverOn)
        {
            StartCoroutine(ReceiveTextFromHttpServer());
        }
    }

    IEnumerator ReceiveTextFromHttpServer()
    {
        while (true)
        {
            UnityWebRequest www = UnityWebRequest.Get("localhost:8080");
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                // Show results as text
                Debug.Log(www.downloadHandler.text);

                // Or retrieve results as binary data
                byte[] results = www.downloadHandler.data;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
