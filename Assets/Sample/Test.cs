using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GPUSkinning;
public class Test : MonoBehaviour
{
    public AnimationPlayer animationPlayer;
    public string ClipName;
    // Start is called before the first frame update
    IEnumerator Start()
    {
        yield return new WaitForSeconds(1f);
        animationPlayer.Play(ClipName);
    }
}
