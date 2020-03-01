using System;
using UnityEngine;
using UnityEngine.UI;

public class FPSMeter : MonoBehaviour {

    public float updatePeriod;
    double time = 0;
    int frames = 0;
    int[] fps;
    float max, min;
    float avg;
    int i = 0;
    bool firstCycle = true;
    public int frameRateLimit = 60;
    int framesNum;
    int textureHeight;
    Texture2D tex;
    Text minFPSText,maxFPSText,avgFPSText;
    // Use this for initialization
    void Start () {
        Image img = transform.GetChild(0).gameObject.GetComponent<Image>(); 
        minFPSText = transform.GetChild(1).gameObject.GetComponent<Text>();
        maxFPSText = transform.GetChild(2).gameObject.GetComponent<Text>();
        avgFPSText = transform.GetChild(3).gameObject.GetComponent<Text>();
        textureHeight = (int)img.rectTransform.rect.height;
        tex = new Texture2D((int)img.rectTransform.rect.width, textureHeight);
        Sprite spr = Sprite.Create(tex, new Rect(0, 0, (int)img.rectTransform.rect.width, textureHeight), new Vector2(0, 0));
        framesNum = (int)img.rectTransform.rect.width;
        fps = new int[framesNum];
        img.sprite = spr;
    }
	
	// Update is called once per frame
	void Update () {
        time += Time.deltaTime;
        frames++;
        if (time > updatePeriod)
        {
            time = 0;
            frames *= (int)(1/updatePeriod);
            fps[i] = frames;
            if (frames > max) max = frames;
            if (frames < min) min = frames;
            if (!firstCycle) {
                for (int ii = 0; ii < framesNum-1; ii++)
                    for (int j = 0; j < textureHeight; j++)
                    {
                        tex.SetPixel(ii, j, tex.GetPixel(ii + 1, j));
                    }
                for (int j = 0; j < textureHeight; j++)
                    tex.SetPixel(framesNum-1, j, Color.gray);
                int y = Convert.ToInt32(frames * textureHeight / frameRateLimit);
                for (int j = 0; j < y && j < textureHeight; j++)
                    tex.SetPixel(framesNum-1, j, Color.blue);
                tex.Apply();
                avg = 0;
                for (int i = 0; i < framesNum; i++)
                    avg += fps[i];
                avg /= framesNum;
            }
            else
            {
                if (i == 0)
                {
                    max = frames;
                    min = frames;
                    avg = frames;
                }
                else
                {
                    avg = 0;
                    for (int j = 0; j <= i; j++)
                        avg += fps[j];
                    avg /= i + 1;
                }
                int y = Convert.ToInt32(frames * textureHeight / frameRateLimit);
                for (int j = 0; j < y && j < textureHeight; j++)
                    tex.SetPixel(i, j, Color.blue);
                tex.Apply();
            }
            frames = 0;
            i++;
            if (i >= framesNum)
            {
                firstCycle = false;
                i = 0;
            }
            avgFPSText.text = "Avg fps: " + avg;
            minFPSText.text = "Min fps: " + min;
            maxFPSText.text = "Max fps:" + max;
        }
	}
}
