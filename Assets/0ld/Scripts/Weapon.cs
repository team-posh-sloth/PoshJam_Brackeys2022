using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Old_Labyrinth
{

    public class Weapon : MonoBehaviour
    {
        [SerializeField] GameObject dispellEffect;
        [SerializeField] float iTime, blinkTime, poofTime;
        bool iFrames, iFramesPlayer;

        [SerializeField] Material flashMaterial;
        Renderer[] renderersToFlash;
        Material[] ogMaterials;

        AudioSource audio;
        Player player;
        private void Start()
        {
            audio = GameObject.Find("Player").GetComponent<AudioSource>();
            player = GameObject.Find("Player").GetComponent<Player>();
        }
        void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent(out MazeEnemy enemy) && gameObject.tag == "Player Weapon" && !iFrames)
            {
                if (enemy.isReal)
                {
                    foreach (MazeEnemy mazeEnemy in enemy.enemySet.GetComponentsInChildren<MazeEnemy>())
                    {
                        mazeEnemy.hitPoints -= 1;
                    }
                    if (enemy.hitPoints <= 0)
                    {
                        enemy.DropToken();
                        foreach (MazeEnemy mazeEnemy in enemy.enemySet.GetComponentsInChildren<MazeEnemy>())
                        {
                            Destroy(mazeEnemy.gameObject);
                        }
                        return;
                    }
                    else
                    {
                        iFrames = true;
                        StartCoroutine(FlashEnemy(enemy.gameObject));
                    }
                }
                else
                {
                    enemy.isPoof = true;
                    audio.volume = 5;
                    audio.pitch = 1;
                    audio.clip = player.poofSound;
                    audio.Play();
                }
                if (enemy.enemySet.GetComponentsInChildren<MazeEnemy>().Length > 1)
                {
                    StartCoroutine(PoofAllHome(enemy.GetComponent<MazeEnemy>()));
                }
            }
            if (other.name == "Player" && gameObject.tag != "Player Weapon" && !iFramesPlayer)
            {
                if (other.GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName("block"))
                {
                    if (audio.isPlaying) { audio.Stop(); }
                    audio.volume = 0.1f;
                    audio.pitch = 1;
                    audio.clip = player.shieldSound;
                    audio.Play();
                }
                else
                {
                    iFramesPlayer = true;
                    StartCoroutine(FlashEnemy(other.gameObject));
                    other.GetComponent<Player>().takeDamage();
                }
            }
        }
        IEnumerator PoofAllHome(MazeEnemy data)
        {
            GameObject enemySet = data.enemySet;
            GameObject[] enemies = new GameObject[enemySet.GetComponentsInChildren<MazeEnemy>().Length];
            float poofTimer = poofTime; // Disappears and reappears after the specified seconds
            int i = 0;
            while (iFrames) // Wait until iframes are disabled before poofing
            {
                yield return null;
            }
            foreach (MazeEnemy enemy in enemySet.GetComponentsInChildren<MazeEnemy>()) // Gets each of the enemies in this enemy's set
            {
                enemies[i] = enemy.gameObject;
                if (enemy.isPoof)
                {
                    GameObject particleEffect = Instantiate(dispellEffect, new Vector3(enemy.transform.position.x, enemy.transform.position.y + 2, enemy.transform.position.z), enemy.transform.rotation);
                    particleEffect.GetComponent<ParticleSystem>().Play();
                    enemy.gameObject.SetActive(false);
                }
                else
                {
                    enemy.GetComponent<CharacterController>().enabled = false; //Must disable character controller to use transform to teleport
                    enemy.transform.localPosition = new Vector3(enemy.homePosition.x, enemy.homePosition.y, enemy.homePosition.z);
                    enemy.GetComponent<CharacterController>().enabled = true;
                    enemy.gameObject.SetActive(false);
                }
                i++;
            }
            while (poofTimer > 0)
            {
                poofTimer -= Time.deltaTime;
                yield return null;
            }
            foreach (GameObject enemy in enemies) // Gets each of the enemies in this enemy's set
            {
                if (!enemy.GetComponent<MazeEnemy>().isPoof)
                {
                    enemy.gameObject.SetActive(true);
                }
            }

            data.ShuffleReal();
        }

        IEnumerator FlashEnemy(GameObject enemy)
        {
            // Save the flashing states of enemy's renderer materials
            if (enemy.GetComponentsInChildren<SkinnedMeshRenderer>().Length < 1)
            {
                renderersToFlash = new MeshRenderer[1]; // Set size of the array to fit amount of renderers
                ogMaterials = new Material[1]; // Set size of the array to fit amount of materials
                for (int i = 0; i < renderersToFlash.Length; i++)
                {
                    renderersToFlash.SetValue(enemy.GetComponent<MeshRenderer>(), i); // Save each of the enemy's skinned mesh renderers
                    ogMaterials.SetValue(renderersToFlash[i].material, i); // Save each of the enemy's materials from the renderers we will flash
                }
            }
            else
            {
                renderersToFlash = new SkinnedMeshRenderer[enemy.GetComponentsInChildren<SkinnedMeshRenderer>().Length]; // Set size of the array to fit amount of renderers
                ogMaterials = new Material[renderersToFlash.Length]; // Set size of the array to fit amount of materials
                for (int i = 0; i < renderersToFlash.Length; i++)
                {
                    renderersToFlash.SetValue(enemy.GetComponentsInChildren<SkinnedMeshRenderer>()[i], i); // Save each of the enemy's skinned mesh renderers
                    ogMaterials.SetValue(renderersToFlash[i].material, i); // Save each of the enemy's materials from the renderers we will flash
                }
            }

            // Flashes for an amount of seconds equal to iTimer
            float blinkDelay = 0;
            for (float i = 0; i <= iTime; i += Time.deltaTime)
            {
                if (blinkDelay <= 0)
                {
                    for (int j = 0; j < renderersToFlash.Length; j++) // Check each of the renderers
                    {
                        if (renderersToFlash[j].material.color != flashMaterial.color) { renderersToFlash[j].material = flashMaterial; } // Blink to the flash material
                        else { renderersToFlash[j].material = ogMaterials[j]; } // Blink to the original material
                    }
                    blinkDelay = blinkTime;
                }
                else { blinkDelay -= Time.deltaTime; }
                yield return null;
            }
            // Return things to the way they should be
            for (int i = 0; i < renderersToFlash.Length; i++)
            {
                renderersToFlash[i].material = ogMaterials[i]; // Blink to the original material
            }
            iFrames = false;
            iFramesPlayer = false;
        }
    }
}