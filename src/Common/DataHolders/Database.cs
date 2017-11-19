﻿//OriginalownerBlockBa5her,devicepublicdomain
using System;using System.IO;using System.IO.Compression;using System.Runtime.Serialization;using System.Runtime.Serialization.Formatters.Binary;namespace EZDatabase{public sealed class Database{private string f;public Database(string file,bool create=true){f=file;if(!create)return;lock(this)File.Open(f,(FileMode)4).Dispose();}public dynamic Read(){lock(this){using(var c=new FileStream(f,FileMode.Open)){if(!c.CanWrite||!c.CanRead)throw new IOException("Invalid permissions to read or write");using(var uc=new MemoryStream()){using(var g=new GZipStream(c,CompressionMode.Decompress,true))g.CopyTo(uc);uc.Position=0;return new BinaryFormatter().Deserialize(uc);}}}}public void Write(dynamic d){lock(this){if(!((object)d).GetType().IsSerializable)throw new SerializationException("The object trying to be serialized is not marked serializable");using(var f2=new FileStream(f,FileMode.Open)){if(!f2.CanWrite||!f2.CanRead)throw new IOException("Invalid permissions to read or write");byte[]b;using(var m=new MemoryStream())using(var g=new GZipStream(m,CompressionMode.Compress)){new BinaryFormatter().Serialize(g,d);g.Close();m.Close();b=m.ToArray();}f2.Write(b,0,b.Length);}}}}}