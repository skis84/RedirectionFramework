﻿/*
The MIT License (MIT)
Copyright (c) 2015 Sebastian Schöner
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using CSUtil.Commons;
using System;
using System.Reflection;

namespace TrafficManager.RedirectionFramework {
	public struct RedirectCallsState {
		public byte A, B, C, D, E;
		public ulong F;
	}

	/// Helper class to deal with detours. This version is for Unity 5 x64 on Windows.
	/// We provide three different methods of detouring.
	
	/// Helper class to deal with detours. This version is for Unity 5 x64 on Windows.
	/// We provide three different methods of detouring.
	/// <summary>
	/// Helper class to deal with detours. This version is for Unity 5 x64 on Windows.
	/// We provide three different methods of detouring.
	/// </summary>
	public static class RedirectionHelper {
		/// <summary>
		/// Redirects all calls from method 'from' to method 'to'.
		/// </summary>
		/// <param name="from"></param>
		/// <param name="to"></param>
		public static RedirectCallsState RedirectCalls(MethodInfo from, MethodInfo to) {
			// GetFunctionPointer enforces compilation of the method.
			var fptr1 = from.MethodHandle.GetFunctionPointer();
			var fptr2 = to.MethodHandle.GetFunctionPointer();

			Log._Debug($"RedirectionHelper.RedirectCalls({from.Name}, {to.Name}) called. IsRedirected1={IsRedirected(fptr1, fptr2)} IsRedirected2={IsRedirected(from.MethodHandle.GetFunctionPointer(), to.MethodHandle.GetFunctionPointer())}");
			Log._Debug($"RedirectionHelper.RedirectCalls({from.Name}, {to.Name}): before redir. fptr1_fnc={GetRedir(fptr1).ArrayToString()} fptr1={(ulong)fptr1.ToInt64()} fptr2={(ulong)fptr2.ToInt64()}");

			RedirectCallsState ret = PatchJumpTo(fptr1, fptr2);

			Log._Debug($"RedirectionHelper.RedirectCalls({from.Name}, {to.Name}): after redir. fptr1_fnc={GetRedir(fptr1).ArrayToString()} fptr1={(ulong)fptr1.ToInt64()} fptr2={(ulong)fptr2.ToInt64()}");
			Log._Debug($"RedirectionHelper.RedirectCalls({from.Name}, {to.Name}) finished. IsRedirected1={IsRedirected(fptr1, fptr2)} IsRedirected2={IsRedirected(from.MethodHandle.GetFunctionPointer(), to.MethodHandle.GetFunctionPointer())}");

			return ret;
		}

		public static void RevertRedirect(MethodInfo from, RedirectCallsState state) {
			var fptr1 = from.MethodHandle.GetFunctionPointer();
			RevertJumpTo(fptr1, state);
		}

		public static bool IsRedirected(MethodInfo from, MethodInfo to) {
			var fptr1 = from.MethodHandle.GetFunctionPointer();
			var fptr2 = to.MethodHandle.GetFunctionPointer();
			return IsRedirected(fptr1, fptr2);
		}

		private static byte[] GetRedir(IntPtr ptr) {
			byte[] ret = new byte[13];
			unsafe
			{
				byte* pointer = (byte*)ptr.ToPointer();
				for (int i = 0; i < 13; ++i) {
					ret[i] = *(pointer + i);
				}
			}
			return ret;
		}

		public static bool IsRedirected(IntPtr site, IntPtr target) {
			unsafe
			{
				byte* sitePtr = (byte*)site.ToPointer();
				return *sitePtr == 0x49 &&
					*(sitePtr + 1) == 0xBB &&
					*((ulong*)(sitePtr + 2)) == (ulong)target.ToInt64() &&
					*(sitePtr + 10) == 0x41 &&
					*(sitePtr + 11) == 0xFF &&
					*(sitePtr + 12) == 0xE3;
			}
		}

		/// <summary>
		/// Primitive patching. Inserts a jump to 'target' at 'site'. Works even if both methods'
		/// callers have already been compiled.
		/// </summary>
		/// <param name="site"></param>
		/// <param name="target"></param>
		public static RedirectCallsState PatchJumpTo(IntPtr site, IntPtr target) {
			RedirectCallsState state = GetState(site);

			// R11 is volatile.
			unsafe
			{
				byte* sitePtr = (byte*)site.ToPointer();

				*sitePtr = 0x49; // mov r11, target
				*(sitePtr + 1) = 0xBB;
				*((ulong*)(sitePtr + 2)) = (ulong)target.ToInt64();
				*(sitePtr + 10) = 0x41; // jmp r11
				*(sitePtr + 11) = 0xFF;
				*(sitePtr + 12) = 0xE3;
			}

			return state;
		}

		public static RedirectCallsState GetState(IntPtr site) {
			RedirectCallsState state = new RedirectCallsState();

			// R11 is volatile.
			unsafe
			{
				byte* sitePtr = (byte*)site.ToPointer();
				state.A = *sitePtr;
				state.B = *(sitePtr + 1);
				state.C = *(sitePtr + 10);
				state.D = *(sitePtr + 11);
				state.E = *(sitePtr + 12);
				state.F = *((ulong*)(sitePtr + 2));
			}

			return state;
		}

		public static void RevertJumpTo(IntPtr site, RedirectCallsState state) {
			unsafe
			{
				byte* sitePtr = (byte*)site.ToPointer();
				*sitePtr = state.A; // mov r11, target
				*(sitePtr + 1) = state.B;
				*((ulong*)(sitePtr + 2)) = state.F;
				*(sitePtr + 10) = state.C; // jmp r11
				*(sitePtr + 11) = state.D;
				*(sitePtr + 12) = state.E;
			}
		}
	}
}
