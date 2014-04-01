AndroidHandlerForDNet
=====================

Do something like Handler in Android for .net platform

```c#
	T1 = new AMCHandlerThread("T1");
	T1.Start();
	AMCLooper lp = T1.getLooper();
	Handler1 h1 = new Handler1(lp);
	h1.Send(new AMCMsg(1));
	h1.Send(new AMCMsg(2), 1000);
	h1.Send(new AMCMsg(3, "Hello Handler."));
	AMCBundle bundle = new AMCBundle();
	bundle.put("key", "THE VALUE");
	h1.Send(new AMCMsg(4, bundle));
```