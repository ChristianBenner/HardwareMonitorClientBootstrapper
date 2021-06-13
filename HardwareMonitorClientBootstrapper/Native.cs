using NativeInterface;
using OpenHardwareMonitor.Hardware;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace HardwareMonitorClientBootstrapper
{
    class NativeInterface
    {
        public const string APPLICATION_CORE_CLASS_PATH = "com/bennero/client/core/ApplicationCore";
        public const string APPLICATION_CORE_LAUNCH_APPLICATION_FUNCTION = "launchApplication";

        public const string NATIVE_CLASS_PATH = "com/bennero/client/bootstrapper/Native";
        public const string NATIVE_UPDATE_SENSORS_FUNCTION = "updateSensors";
        public const string NATIVE_ADD_SENSORS_FUNCTION = "addSensors";

        public const string SENSOR_REQUEST_CLASS_PATH = "com/bennero/client/bootstrapper/SensorRequest";
        public const string SENSOR_REQUEST_SET_VALUE_FUNCTION = "setValue";


        private const byte VOLTAGE = 0x01;
        private const byte CLOCK = 0x02;
        private const byte TEMPERATURE = 0x03;
        private const byte LOAD = 0x04;
        private const byte FAN = 0x05;
        private const byte FLOW = 0x06;
        private const byte CONTROL = 0x07;
        private const byte LEVEL = 0x08;
        private const byte FACTOR = 0x09;
        private const byte POWER = 0x0A;
        private const byte DATA = 0x0B;
        private const byte SMALL_DATA = 0x0C;
        private const byte THROUGHPUT = 0x0D;

        class Updator : IVisitor
        {
            public void VisitComputer(IComputer computer)
            {
                computer.Traverse(this);
            }

            public void VisitHardware(IHardware hardware)
            {
                hardware.Update();

                foreach (IHardware subHardware in hardware.SubHardware)
                {
                    subHardware.Accept(this);
                }
            }

            public void VisitSensor(ISensor sensor)
            {
                throw new NotImplementedException();
            }

            public void VisitParameter(IParameter parameter)
            {
                throw new NotImplementedException();
            }
        }

        class JniSensor
        {
            public ISensor sensor;
            public IntPtr jniObjRef;

            public JniSensor(ISensor sensor, IntPtr jniObjRef)
            {
                this.sensor = sensor;
                this.jniObjRef = jniObjRef;
            }
        }

        private Jvm jvm;
        private Computer computer;
        private Updator updateVisitor;
        private int idCount = 0;

        private Dictionary<IntPtr, ISensor> sensorObjects;

        public static class NativeCallbackDelegates
        {
            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate void UpdateNative(IntPtr env, IntPtr clazz);

            [UnmanagedFunctionPointer(CallingConvention.StdCall)]
            public delegate void NativeAddSensors(IntPtr env, IntPtr clazz);
        }

        private static Delegate addSensorsDelegate;
        private static Delegate updateNativeDelegate;
        
        private NativeJavaMethod[] methods;

        public NativeInterface(Jvm jvm)
        {
            this.jvm = jvm;
            updateVisitor = new Updator();
            sensorObjects = new Dictionary<IntPtr, ISensor>();

            computer = new Computer();
            computer.FanControllerEnabled = true;
            computer.CPUEnabled = true;
            computer.GPUEnabled = true;
            computer.HDDEnabled = true;
            computer.MainboardEnabled = true;
            computer.RAMEnabled = true;
            computer.Open();

            // Update/fetch and add sensors
            UpdateSensors();

            addSensorsDelegate = (NativeCallbackDelegates.NativeAddSensors)NativeAddSensors;
            updateNativeDelegate = (NativeCallbackDelegates.UpdateNative)UpdateNative;

            GC.KeepAlive(addSensorsDelegate);
            GC.KeepAlive(updateNativeDelegate);

            methods = new NativeJavaMethod[]
            {
                NativeJavaMethod.DescribeNativeVoidMethod(NATIVE_ADD_SENSORS_FUNCTION, addSensorsDelegate),
                NativeJavaMethod.DescribeNativeVoidMethod(NATIVE_UPDATE_SENSORS_FUNCTION, updateNativeDelegate)
            };

            jvm.RegisterNatives(NATIVE_CLASS_PATH, methods);
        }

        public unsafe void RunClient()
        {
            Console.WriteLine("Launching Hardware Monitor Editor");
            JavaMethod nativeLaunch = jvm.GetStaticVoidMethod(APPLICATION_CORE_CLASS_PATH, APPLICATION_CORE_LAUNCH_APPLICATION_FUNCTION);
            jvm.CallStaticVoidMethod(nativeLaunch);
        }

        private byte getSensorType(ISensor sensor)
        {
            switch(sensor.SensorType)
            {

                case SensorType.Voltage:
                    return VOLTAGE;
                case SensorType.Clock:
                    return CLOCK;
                case SensorType.Temperature:
                    return TEMPERATURE;
                case SensorType.Load:
                    return LOAD;
                case SensorType.Fan:
                    return FAN;
                case SensorType.Flow:
                    return FLOW;
                case SensorType.Control:
                    return CONTROL;
                case SensorType.Level:
                    return LEVEL;
                case SensorType.Factor:
                    return FACTOR;
                case SensorType.Power:
                    return POWER;
                case SensorType.Data:
                    return DATA;
                case SensorType.SmallData:
                    return SMALL_DATA;
                case SensorType.Throughput:
                    return THROUGHPUT;
            }

            return LOAD;
        }

        private void UpdateSensors()
        {
            computer.Accept(updateVisitor);
        }

        public unsafe void NativeAddSensors(IntPtr env, IntPtr clazz)
        {
            // Attach to the current thread in order to work correctly
            jvm.AttachCurrentThread();

            for (int i = 0; i < computer.Hardware.Length; i++)
            {
                IHardware hardware = computer.Hardware[i];

                for (int j = 0; j < hardware.Sensors.Length; j++)
                {
                    ISensor sensor = hardware.Sensors[j];

                    ParameterBuilder builder = new ParameterBuilder().AddInt(idCount++).AddString(jvm, sensor.Name).
                        AddFloat(sensor.Max.Value).AddByte(getSensorType(sensor)).AddString(jvm, sensor.Hardware.Name).
                        AddFloat(sensor.Value.Value);

                    // Construct sensor request object with parameters
                    IntPtr sensorObjectParam = jvm.CreateObject(SENSOR_REQUEST_CLASS_PATH,
                        builder.GetParameterString(), builder.Build());
                    sensorObjects.Add(sensorObjectParam, sensor);
                }
            }
        }
        
        public unsafe void UpdateNative(IntPtr env, IntPtr clazz)
        {
            UpdateSensors();
            foreach (var pair in sensorObjects)
            {
                ParameterBuilder builder = new ParameterBuilder().AddFloat(pair.Value.Value.Value);

                // // Populate the sensor object
                JavaMethod setValueMethod = jvm.GetMethod(SENSOR_REQUEST_CLASS_PATH,
                    SENSOR_REQUEST_SET_VALUE_FUNCTION,
                    builder.GetParameterString());

                jvm.CallVoidMethod(pair.Key, setValueMethod, builder.Build());
            }
        }
    }
}
