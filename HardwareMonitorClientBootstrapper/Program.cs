using NativeInterface;
using System;
using System.IO;

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

            if(!File.Exists(jvmPath))
            {
                Console.Error.WriteLine("JVM not found at path: " + jvmPath);
                Environment.Exit(1);
            }

            if (!File.Exists(jarPath))
            {
                Console.Error.WriteLine("HardwareMonitorEditor not found at path: " + jarPath);
                Environment.Exit(2);
            }

            // Create JVM
            Jvm jvm = new Jvm(jvmPath, Jvm.JNI_VERSION_9, jarPath);
            
            // Create the native interface that handles communication to and from the JAR application
            NativeInterface native = new NativeInterface(jvm);
            native.RunClient();
        }
    }
}
