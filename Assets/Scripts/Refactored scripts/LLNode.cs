using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using UnityEngine;

public class LLNode<Cart> : MonoBehaviour
{
    public int count;
    public Cart first;
    public Cart last;
    // Start is called before the first frame update
    void Start()
    {
        if (first == null)
        {
            count = 0;
        }
        else
        {
            count = 1;
            last = first;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
