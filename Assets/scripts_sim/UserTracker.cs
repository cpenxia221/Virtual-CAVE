using UnityEngine;
using System;
using NetMQ;
using NetMQ.Sockets;
using System.Collections;
using System.Collections.Generic;

namespace PubSub {
    public class UserTracker : MonoBehaviour
    {
        [Serializable]
        public struct Orientation
        {
            public Pos pos;
            public Quat quat;
        }
        [Serializable]
        public struct Pos
        {
            public float x;
            public float y;
            public float z;
        }
        [Serializable]
        public struct Quat
        {
            public float w;
            public float x;
            public float y;
            public float z;

        }
        [SerializeField] private string host;
        [SerializeField] private string port;
        [SerializeField] public float factor = 1; 

        private Orientation orientation;
        private Vector3 user_pos;
        private Quaternion user_rot;
        private SubscriberSocket subSocket;

        private Queue<Orientation> orientation_queue;
        private int orientation_max_size = 5;
        private Orientation queue_average;
        private Orientation previous;
         

        // Start is called before the first frame update
        void Start()
        {
            orientation_queue = new Queue<Orientation>();
            user_pos = new Vector3(0,0,0);
            user_rot = new Quaternion(0,0,0,0);
            // Needed to handle socket exception on succesful socket connection
            AsyncIO.ForceDotNet.Force();
            subSocket = new SubscriberSocket();
            subSocket.Options.ReceiveHighWatermark = 1000; 
            Debug.Log($"Connecting Sub Socket to address: tcp://{host}:{port}");       
            subSocket.Connect($"tcp://{host}:{port}");
            subSocket.SubscribeToAnyTopic();
        }

        private void remove_value_from_average(Orientation value)
        {
            queue_average.pos.x -= value.pos.x / orientation_max_size;
            queue_average.pos.y -= value.pos.y / orientation_max_size;
            queue_average.pos.z -= value.pos.z / orientation_max_size;

            queue_average.quat.x -= value.quat.x / orientation_max_size;
            queue_average.quat.y -= value.quat.y / orientation_max_size;
            queue_average.quat.z -= value.quat.z / orientation_max_size;
            queue_average.quat.w -= value.quat.w / orientation_max_size;
        }

        private void add_value_to_average(Orientation value)
        {
            queue_average.pos.x += value.pos.x / orientation_max_size;
            queue_average.pos.y += value.pos.y / orientation_max_size;
            queue_average.pos.z += value.pos.z / orientation_max_size;

            queue_average.quat.x += value.quat.x / orientation_max_size;
            queue_average.quat.y += value.quat.y / orientation_max_size;
            queue_average.quat.z += value.quat.z / orientation_max_size;
            queue_average.quat.w += value.quat.w / orientation_max_size;
        }

        // Update is called once per frame
        private void Update()
        {   
          
            if (subSocket.TryReceiveFrameString(out var message)){
                   // Debug.Log(message);
                    try{ 
                        orientation = JsonUtility.FromJson<Orientation>(message);
                    
                    
                    if (orientation_queue.Count == orientation_max_size)
                    {
                        Orientation removed_value = orientation_queue.Dequeue();
                        remove_value_from_average(removed_value);
                    }
            
                    
                    
                    orientation_queue.Enqueue(orientation);
                    add_value_to_average(orientation);
                    if(previous.pos.x == 0 && previous.pos.y == 0 && previous.pos.z == 0){
                        previous.pos.x = queue_average.pos.x;
                        previous.pos.y = queue_average.pos.y;
                        previous.pos.z = queue_average.pos.z;
                         Debug.Log("Initialized" + previous.pos.x);
                    }  
                    else{
                        user_pos.x = Math.Max((queue_average.pos.x-previous.pos.x)/factor,0);
                        user_pos.y = 0;
                        user_pos.z =  Math.Max((queue_average.pos.z-previous.pos.z)/factor,0);
                        Debug.Log("x" + (user_pos.x));
                        gameObject.transform.position +=  user_pos;
                        previous.pos.x = queue_average.pos.x;
                        previous.pos.y = queue_average.pos.y;
                        previous.pos.z = queue_average.pos.z;
                    }
                       // user_rot.x = queue_average.quat.x/factor;
                        //user_rot.y = queue_average.quat.y/factor;
                       // user_rot.z = queue_average.quat.z/factor;
                      //  user_rot.w = queue_average.quat.w/factor;

                }
                    catch{
                        Debug.Log($"Failed to Parse Message: {message}");
                    }


}
        }

    void OnApplicationQuit()
    {
        subSocket.Close();
        NetMQConfig.Cleanup();
        Debug.Log("Application ending after " + Time.time + " seconds");
    }
    }
}
