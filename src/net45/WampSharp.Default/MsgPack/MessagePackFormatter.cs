﻿using System;
using System.Linq;
using Json2Msgpack;
using MsgPack;
using MsgPack.Serialization;
using Newtonsoft.Json.Linq;
using WampSharp.Core.Serialization;

namespace WampSharp.MsgPack
{
    public class MessagePackFormatter : IWampFormatter<MessagePackObject>
    {
        private readonly SerializationContext mSerializationContext;

        public MessagePackFormatter()
            : this(new SerializationContext
                {
                    SerializationMethod = SerializationMethod.Map
                })
        {
        }

        public MessagePackFormatter(SerializationContext serializationContext)
        {
            mSerializationContext = serializationContext;

            mSerializationContext.Serializers.Register(new JTokenMessagePackSerializer<JToken>(mSerializationContext));
            mSerializationContext.Serializers.Register(new JTokenMessagePackSerializer<JValue>(mSerializationContext));
            mSerializationContext.Serializers.Register(new JTokenMessagePackSerializer<JArray>(mSerializationContext));
            mSerializationContext.Serializers.Register(new JTokenMessagePackSerializer<JObject>(mSerializationContext));
        }

        public bool CanConvert(MessagePackObject argument, Type type)
        {
            // Irrelavent
            return true;
        }

        public TTarget Deserialize<TTarget>(MessagePackObject message)
        {
            MessagePackSerializer<TTarget> serializer = 
                mSerializationContext.GetSerializer<TTarget>();
            
            MessagePackSerializer<MessagePackObject> messageSerializer = 
                mSerializationContext.GetSerializer<MessagePackObject>();

            byte[] bytes = 
                messageSerializer.PackSingleObject(message);

            TTarget result = serializer.UnpackSingleObject(bytes);

            return result;
        }

        public object Deserialize(Type type, MessagePackObject message)
        {
            IMessagePackSingleObjectSerializer serializer =
                mSerializationContext.GetSerializer(type);

            MessagePackSerializer<MessagePackObject> messageSerializer =
                mSerializationContext.GetSerializer<MessagePackObject>();

            byte[] bytes =
                messageSerializer.PackSingleObject(message);

            object result = serializer.UnpackSingleObject(bytes);

            return result;
        }

        public MessagePackObject Serialize(object value)
        {
            // Workaround:
            object[] asArray = value as object[];

            if (asArray == null)
            {
                return SimpleSerialize(value);
            }
            else
            {
                return SerializeArray(asArray);
            }
        }

        private MessagePackObject SerializeArray(object[] asArray)
        {
            MessagePackObject[] resultArray = 
                asArray.Select(x => Serialize(x))
                .ToArray();

            MessagePackSerializer<MessagePackObject[]> messageSerializer =
                mSerializationContext.GetSerializer<MessagePackObject[]>();

            byte[] bytes =
                messageSerializer.PackSingleObject(resultArray);

            MessagePackSerializer<MessagePackObject> serializer =
                mSerializationContext.GetSerializer<MessagePackObject>();

            MessagePackObject result = serializer.UnpackSingleObject(bytes);

            return result;
        }

        private MessagePackObject SimpleSerialize(object value)
        {
            IMessagePackSingleObjectSerializer serializer =
                mSerializationContext.GetSerializer(value.GetType());

            var bytes =
                serializer.PackSingleObject(value);

            MessagePackSerializer<MessagePackObject> messageSerializer =
                mSerializationContext.GetSerializer<MessagePackObject>();

            MessagePackObject serialized =
                messageSerializer.UnpackSingleObject(bytes);

            return serialized;
        }
    }
}