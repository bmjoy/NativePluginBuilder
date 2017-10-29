﻿using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace iBicha
{
	public class AndroidBuilder : PluginBuilderBase {
		public override void PreBuild (NativePlugin plugin, NativeBuildOptions buildOptions){
			base.PreBuild (plugin, buildOptions);

			if (buildOptions.BuildTarget != BuildTarget.Android) {
				throw new System.ArgumentException (string.Format(
					"BuildTarget mismatch: expected:\"{0}\", current:\"{1}\"", BuildTarget.Android, buildOptions.BuildTarget));
			}

			if (buildOptions.Architecture != Architecture.arm && buildOptions.Architecture != Architecture.x86) {
				throw new System.NotSupportedException (string.Format(
					"Architecture not supported: only ARMv7 and x86, current:\"{0}\"", buildOptions.Architecture));
			}

			if (buildOptions.BuildType == BuildType.Default) {
				buildOptions.BuildType = EditorUserBuildSettings.development ? BuildType.Debug : BuildType.Release;
			}

			if (buildOptions.BuildType != BuildType.Debug && buildOptions.BuildType != BuildType.Release) {
				throw new System.NotSupportedException (string.Format(
					"BuildType not supported: only Debug and Release, current:\"{0}\"", buildOptions.BuildType));
			}

			if (string.IsNullOrEmpty(GetNDKLocation())) {
				throw new System.Exception ("Missing Android NDK.");
			}
		}

		public override BackgroundProcess Build (NativePlugin plugin, NativeBuildOptions buildOptions)
		{
			StringBuilder cmakeArgs = GetBasePluginCMakeArgs (plugin);

			AddCmakeArg (cmakeArgs, "CMAKE_BUILD_TYPE", buildOptions.BuildType.ToString());

			cmakeArgs.AppendFormat ("-G {0} ", "\"Unix Makefiles\"");
			AddCmakeArg (cmakeArgs, "ANDROID", "ON", "BOOL");

			string ndkLocation = GetNDKLocation();
			AddCmakeArg (cmakeArgs, "ANDROID_NDK", ndkLocation, "PATH");

			string toolchain = CombinePath(ndkLocation, "build/cmake/android.toolchain.cmake");
			AddCmakeArg (cmakeArgs, "CMAKE_TOOLCHAIN_FILE", "\"" + toolchain + "\"", "FILEPATH");

			string archName = buildOptions.Architecture == Architecture.arm ? "armeabi-v7a" : "x86";
			AddCmakeArg (cmakeArgs, "ANDROID_ABI", archName);
			cmakeArgs.AppendFormat ("-B{0}/{1} ", "Android", archName);
			//Do we need to target a specific api?
			if (buildOptions.AndroidSdkVersion > 0) {
				AddCmakeArg (cmakeArgs, "ANDROID_PLATFORM", "android-" + buildOptions.AndroidSdkVersion);
			}
				
			buildOptions.OutputDirectory = CombinePath (plugin.buildFolder, "Android", archName);

			ProcessStartInfo startInfo = new ProcessStartInfo();
			startInfo.FileName = CMakeHelper.GetCMakeLocation ();
			startInfo.Arguments = cmakeArgs.ToString();
			startInfo.WorkingDirectory = plugin.buildFolder;

			BackgroundProcess process = new BackgroundProcess (startInfo);
			process.Name = string.Format ("Building {0} for {1} ({2})", plugin.Name, "Android", archName);
			return process;

		}

		public override BackgroundProcess Install (NativePlugin plugin, NativeBuildOptions buildOptions)
		{
			BackgroundProcess process = base.Install (plugin, buildOptions);
			string archName = buildOptions.Architecture == Architecture.arm ? "armeabi-v7a" : "x86";
			process.Name = string.Format ("Installing {0} for {1} ({2})", plugin.Name, "Android", archName);
			return process;
		}


		public override void PostBuild (NativePlugin plugin, NativeBuildOptions buildOptions)
		{
			base.PostBuild (plugin, buildOptions);
		}
	


		private static string GetNDKLocation()
		{
			//Get the default location
			string sdk = GetSDKLocation();
			string ndk = CombinePath(sdk, "ndk-bundle");
			if (Directory.Exists(ndk))
			{
				return ndk;
			}
			//Get ndk from Unity settings
			return EditorPrefs.GetString("AndroidNdkRoot");
		}

		private static string GetSDKLocation()
		{
			//Get the default location
			return EditorPrefs.GetString("AndroidSdkRoot");
		}

	}
}