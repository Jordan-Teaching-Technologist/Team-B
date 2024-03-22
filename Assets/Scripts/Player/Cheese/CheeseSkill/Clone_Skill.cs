// using System;
// using System.Collections;
// using System.Collections.Generic;
// using CheeseController;
// using Photon.Pun;
// using UnityEngine;
//
// public class Clone_Skill : MonoBehaviourPunCallbacks
// {
//     public float cooldown = 30f;
//     private bool isCooldown = false;
//     private float skillIconFill;
//     private float nextAvailableTime;
//
//     void Update()
//     {
//         if (photonView.IsMine)
//         {
//             if (Input.GetKeyDown(KeyCode.F) && !isCooldown)
//             {
//                 Clone();
//                 StartCooldown();
//             }
//
//             if (isCooldown)
//             {
//                 skillIconFill = (nextAvailableTime - Time.time) / cooldown;
//                 skillIconFill = Mathf.Clamp(skillIconFill, 0f, 1f); 
//                 UpdateIcon();
//                 
//                 if (Time.time >= nextAvailableTime)
//                 {
//                     isCooldown = false;
//                     skillIconFill = 0f; 
//                     UpdateIcon();
//                 }
//             }
//         }
//     }
//
//     void Clone()
//     {
//         Debug.Log("Attempting to clone character.");
//         // GameObject clone = PhotonNetwork.Instantiate(this.gameObject.name.Replace("(Clone)",""), transform.position, transform.rotation);
//         GameObject clone = PhotonNetwork.Instantiate("Clone", transform.position, transform.rotation);
//         var cloneMovement = clone.AddComponent<CloneMovement>();
//         cloneMovement.moveSpeed = 100f; 
//         cloneMovement.turnSpeed = 300f; 
//
//         StartCoroutine(DestroyNetworkObject(clone, 10f));
//     }
//     IEnumerator DestroyNetworkObject(GameObject target, float delay)
//     {
//         yield return new WaitForSeconds(delay);
//         PhotonNetwork.Destroy(target);
//     }
//
//     void StartCooldown()
//     {
//         isCooldown = true;
//         nextAvailableTime = Time.time + cooldown; 
//         skillIconFill = 1f;
//         UpdateIcon();
//     }
//     private void UpdateIcon()
//     {
//         if (CheeseFightUI.Instance != null)
//         {
//             CheeseFightUI.Instance.UpdateSkill_Icon(skillIconFill);
//         } 
//     }
// }


using System.Collections;
using UnityEngine;
using Photon.Pun;

public class Clone_Skill : MonoBehaviourPunCallbacks
{
    public float Skill_Duration = 10f; // Clone duration
    private bool skillUsed = false;
    private float skillTimer; // Timer to track skill duration

    void Update()
    {
        if (photonView.IsMine && !skillUsed && Input.GetKeyDown(KeyCode.F))
        {
            StartCoroutine(ApplyCloneEffect());
        }

        // Continuously update the skill icon if the skill has been used
        if (skillUsed)
        {
            UpdateIcon(skillTimer / Skill_Duration);
        }
    }

    IEnumerator ApplyCloneEffect()
    {
        skillUsed = true; // Mark the skill as used

        Clone(); // Create the clone

        skillTimer = Skill_Duration; // Reset the skill timer

        while (skillTimer > 0)
        {
            skillTimer -= Time.deltaTime;
            UpdateIcon(skillTimer / Skill_Duration);
            yield return null;
        }
        
        CheeseFightUI.Instance.UpdateSkill_Icon(1f);
        GetComponent<GetSkill>().DeactivateSkill("Clone Skill"); 
        skillUsed = false; 
    }

         void Clone()
     {
         Debug.Log("Attempting to clone character.");
         // GameObject clone = PhotonNetwork.Instantiate(this.gameObject.name.Replace("(Clone)",""), transform.position, transform.rotation);
         GameObject clone = PhotonNetwork.Instantiate("Clone", transform.position, transform.rotation);
         var cloneMovement = clone.AddComponent<CloneMovement>();
         cloneMovement.moveSpeed = 100f; 
         cloneMovement.turnSpeed = 300f; 

         StartCoroutine(DestroyNetworkObject(clone, Skill_Duration));
     }

    IEnumerator DestroyNetworkObject(GameObject target, float delay)
    {
        yield return new WaitForSeconds(delay);
        PhotonNetwork.Destroy(target);
    }
    private void UpdateIcon(float fillAmount)
    {
        if (CheeseFightUI.Instance != null)
        {
            CheeseFightUI.Instance.UpdateSkill_Icon(fillAmount);
        }
    }
}
