/**
 * MaterialColorRandomizerNetwork.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2/26/2021 (en-US)
 */

using Mirror;
using System.Collections;
using UnityEngine;

public class MaterialColorRandomizerNetwork : NetworkBehaviour
{
    static readonly int MaterialColorId = Shader.PropertyToID("_BaseColor");

    [SerializeField]
    GameObject authorityObject;
    [SerializeField]
    MeshRenderer randomizedMaterialOwner;
    [SerializeField, Range(.25f, 3)]
    float randomizeDelay = .5f;

    [SyncVar(hook = nameof(UpdateColor))]
    Color materialColor;

    public override void OnStartLocalPlayer()
    {
        authorityObject.SetActive(true);
        RandomizeColor();
    }

    [Command]
    public void CmdRandomizeColor()
    {
        materialColor = Random.ColorHSV();
    }

    void UpdateColor(Color formerColor, Color newColor)
    {
        materialColor = newColor;
        randomizedMaterialOwner.material.SetColor(MaterialColorId, newColor);
    }

    Coroutine RandomizeColor()
    {
        return StartCoroutine(randomizeColorRoutine());
        IEnumerator randomizeColorRoutine()
        {
            var delay = new WaitForSeconds(randomizeDelay);
            while (true)
            {
                yield return delay;
                CmdRandomizeColor();
            }
        }
    }
}