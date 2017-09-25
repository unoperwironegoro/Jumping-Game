﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class LandingController : MonoBehaviour {
    public Vector2 anchorPoint;

    private SpriteRenderer[] srs;
    private Color[] cols;
    private const int sinkOffset = 500;
    private const float sinkScale = 0.8f;

    private Rigidbody2D rb2d;
    
    public GameObject splashParticleSystem;
    public AudioClip splashSound;

    private AnchoredJoint2D grounding;

    private List<GameObject> landObjects = new List<GameObject>();

    public State state;
    public bool airborne { get { return state == State.AIRBORNE; } }
    public bool grounded { get { return state == State.GROUNDED; } }
    public bool submerged { get { return state == State.SUBMERGED; } }

    public delegate void StateChange();
    public event StateChange onSink;
    public event StateChange onSurface;
    public event StateChange onLand;

    public enum State {
        GROUNDED,
        SUBMERGED,
        AIRBORNE
    }

    void Awake() {
        srs = gameObject.GetComponentsInChildren<SpriteRenderer>();
        cols = new Color[srs.Length];
        rb2d = GetComponent<Rigidbody2D>();
    }

    void Start() {
        if (grounded) {
            Invoke("TryLand", 0.1f);
        }
    }
    public bool TryLand() {
        if (CanLand()) {
            Ground(landObjects[0]);
            if(onLand != null) {
                onLand();
            }
            return true;
        } else {
            Sink();
            return false;
        }
    }

    public void Unground() {
        if (grounding) {
            Destroy(grounding);
        }
        state = State.AIRBORNE;
    }

    private void Ground(GameObject ground) {
        grounding = ground.AddComponent<HingeJoint2D>();
        grounding.autoConfigureConnectedAnchor = true;
        Quaternion invrot = Quaternion.Inverse(ground.transform.rotation);
        grounding.anchor = invrot * ((transform.position + (Vector3)anchorPoint) - ground.transform.position);
        grounding.connectedBody = rb2d;
        grounding.enableCollision = true;
        state = State.GROUNDED;
    }

    public bool CanLand() {
        if (landObjects.Count == 0) {
            return false;
        } else {
            return true;
        }
    }

    public void Sink() {
        if (state == State.SUBMERGED) {
            return;
        }
        // Attempt to fix the mysterious "glass floor" bug
        //landObjects.Clear();

        Unground();
        state = State.SUBMERGED;
        
        transform.localScale *= sinkScale;
        Instantiate(splashParticleSystem, transform.position, Quaternion.identity);
        AudioSource.PlayClipAtPoint(splashSound, transform.position);
        for (int i = 0; i < srs.Length; i++) {
            cols[i] = srs[i].color;
            srs[i].color = Color.grey;
            
            srs[i].sortingOrder -= sinkOffset;
        }

        if(onSink != null) {
            onSink();
        }
    }

    public void Surface() {
        transform.localScale /= sinkScale;
        for (int i = 0; i < srs.Length; i++) {
            srs[i].color = cols[i];
            srs[i].sortingOrder += sinkOffset;
        }

        if(onSurface != null) {
            onSurface();
        }
    }

    void OnCollisionEnter2D(Collision2D coll) {
        Unground();
    }

    void OnTriggerEnter2D(Collider2D coll) {
        if (coll.gameObject.layer == LayerMask.NameToLayer("Landable")) {
            landObjects.Add(coll.gameObject);
        }
    }

    void OnTriggerExit2D(Collider2D coll) {
        if (coll.gameObject.layer == LayerMask.NameToLayer("Landable")) {
            landObjects.Remove(coll.gameObject);

            if(grounded && !CanLand()) {
                Sink();
            }
        }
    }
}
