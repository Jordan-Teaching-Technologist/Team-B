using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDetector : MonoBehaviour
{
    public Material outlineMaterial; 
    public float cooldownTime = 30f;
    private bool isDetecting = false; 
    private bool isCooldown = false;
    private float skillIconFill;
    private float nextAvailableTime;
    

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R) && !isDetecting && !isCooldown)
        {
            Debug.Log("R is pressed");
            StartCoroutine(DetectEnemies());
            StartCooldown();
        }
        if (isCooldown)
        {
            skillIconFill = (nextAvailableTime - Time.time) / cooldownTime;
            skillIconFill = Mathf.Clamp(skillIconFill, 0f, 1f); // 确保在0和1之间
            UpdateIcon();
            
            if (Time.time >= nextAvailableTime)
            {
                isCooldown = false;
                skillIconFill = 0f;
                UpdateIcon(); 
            }
        }
    }

    IEnumerator DetectEnemies()
    {
        isDetecting = true;
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, GetComponent<SphereCollider>().radius, LayerMask.GetMask("Human"));
        foreach (var hitCollider in hitColliders)
        {
            SkinnedMeshRenderer[] renderers = hitCollider.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var renderer in renderers)
            {
                Material[] originalMaterials = renderer.materials; 
                Material[] outlineMaterials = new Material[renderer.materials.Length];
                for (int i = 0; i < outlineMaterials.Length; i++)
                {
                    outlineMaterials[i] = outlineMaterial;
                }
                renderer.materials = outlineMaterials;
                StartCoroutine(ResetMaterialAfterDelay(renderer, originalMaterials, 10));
            }
        }
        yield return new WaitForSeconds(10);
        isDetecting = false;
    }

    IEnumerator ResetMaterialAfterDelay(Renderer renderer, Material[] originalMaterials, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (renderer != null)
        {
            renderer.materials = originalMaterials;
        }
    }

    void StartCooldown()
    {
        isCooldown = true;
        nextAvailableTime = Time.time + cooldownTime;
        skillIconFill = 1f;
        UpdateIcon();
    }
    
    private void UpdateIcon()
    {
        if (FightUI3.Instance != null)
        {
            FightUI3.Instance.UpdateSkill_Icon(skillIconFill);
        } 
    }
}