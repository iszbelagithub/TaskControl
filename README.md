# TaskControl
It handles the chained control flow of synchronous/asynchronous tasks.



Description:

The package implements a mechanism for chaining async and sync tasks to execute sequentially.

Async tasks are executed concurrently.
Whereas async-  sync, and sync tasks are alternated, ensuring that each task waits for the completion of the previous task(s) before starting.

Example of the process:


![image](https://github.com/user-attachments/assets/60006c0c-881c-4e39-bec9-ad1c3f70dbf1)

This makes it easy to make non-thread-safe processes thread-safe.

![image](https://github.com/user-attachments/assets/e5f14b0c-9b85-4655-8c4d-337b41dfc9cb)

