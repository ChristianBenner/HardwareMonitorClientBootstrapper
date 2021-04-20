using NativeInterface;
using System;

namespace HardwareMonitorClientBootstrapper
{
    class Program
    {
        static void Main(string[] args)
        {
            // Location of the Java Virtual Machine
            string jvmPath = AppDomain.CurrentDomain.BaseDirectory + "jre\\bin\\server\\jvm.dll";

            // Location of the hardware monitor editor JAR application
            string jarPath = AppDomain.CurrentDomain.BaseDirectory + "HardwareMonitorEditor.jar";

            // Create JVM
            Jvm jvm = new Jvm(jvmPath, Jvm.JNI_VERSION_9, jarPath);
            
            // Create the native interface that handles communication to and from the JAR application
            NativeInterface native = new NativeInterface(jvm);
            native.RunClient();
        }
    }
}
