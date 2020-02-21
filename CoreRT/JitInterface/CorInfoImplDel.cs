using System;
using System.Runtime.InteropServices;

namespace CoreRT.JitInterface
{
    public struct CorInfoImplTest
    {
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate uint GetMethodAttribsDelegate(IntPtr _this, IntPtr ftn);
        public GetMethodAttribsDelegate GetMethodAttribs { get; }
        
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void SetMethodAttribsDelegate(IntPtr _this, IntPtr ftn, CorInfoMethodRuntimeFlags attribs);
        public SetMethodAttribsDelegate SetMethodAttribs;

        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void GetMethodSigDelegate(IntPtr _this, IntPtr ftn, IntPtr sig, IntPtr memberParent);
        public GetMethodSigDelegate GetMethodSig;
        
        [UnmanagedFunctionPointer(default(CallingConvention))]
        [return: MarshalAs(UnmanagedType.I1)] 
        public delegate bool GetMethodInfoDelegate(IntPtr _this, IntPtr ftn, IntPtr info);
        public GetMethodInfoDelegate GetMethodInfo;

        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate CorInfoInline CanInlineDelegate(IntPtr _this, IntPtr callerHnd, IntPtr calleeHnd, ref uint pRestrictions);
        public CanInlineDelegate CanInline;
        
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void ReportInliningDecisionDelegate(IntPtr _this, IntPtr inlinerHnd, IntPtr inlineeHnd, CorInfoInline inlineResult, IntPtr reason);
        public ReportInliningDecisionDelegate ReportInliningDecision;

        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        [return: MarshalAs(UnmanagedType.I1)] 
        public delegate bool CanTailCallDelegate(IntPtr _this, IntPtr callerHnd, IntPtr declaredCalleeHnd, IntPtr exactCalleeHnd, [MarshalAs(UnmanagedType.I1)]bool fIsTailPrefix);
        public CanTailCallDelegate CanTailCall;

        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __reportTailCallDecision(IntPtr _this, IntPtr callerHnd, IntPtr calleeHnd, [MarshalAs(UnmanagedType.I1)]bool fIsTailPrefix, CorInfoTailCall tailCallResult, IntPtr reason);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __getEHinfo(IntPtr _this, IntPtr ftn, uint EHnumber, ref CORINFO_EH_CLAUSE clause);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getMethodClass(IntPtr _this, IntPtr method);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getMethodModule(IntPtr _this, IntPtr method);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __getMethodVTableOffset(IntPtr _this, IntPtr method, ref uint offsetOfIndirection, ref uint offsetAfterIndirection, [MarshalAs(UnmanagedType.U1)] ref bool isRelative);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __resolveVirtualMethod(IntPtr _this, IntPtr virtualMethod, IntPtr implementingClass, IntPtr ownerType);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getUnboxedEntry(IntPtr _this, IntPtr ftn, IntPtr requiresInstMethodTableArg);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getDefaultEqualityComparerClass(IntPtr _this, IntPtr elemType);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __expandRawHandleIntrinsic(IntPtr _this, ref CORINFO_RESOLVED_TOKEN pResolvedToken, ref CORINFO_GENERICHANDLE_RESULT pResult);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate CorInfoIntrinsics __getIntrinsicID(IntPtr _this, IntPtr method, IntPtr pMustExpand);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        [return: MarshalAs(UnmanagedType.I1)] delegate bool __isIntrinsicType(IntPtr _this, IntPtr classHnd);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate CorInfoUnmanagedCallConv __getUnmanagedCallConv(IntPtr _this, IntPtr method);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        [return: MarshalAs(UnmanagedType.Bool)] delegate bool __pInvokeMarshalingRequired(IntPtr _this, IntPtr method, IntPtr callSiteSig);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        [return: MarshalAs(UnmanagedType.Bool)] delegate bool __satisfiesMethodConstraints(IntPtr _this, IntPtr parent, IntPtr method);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        [return: MarshalAs(UnmanagedType.Bool)] delegate bool __isCompatibleDelegate(IntPtr _this, IntPtr objCls, IntPtr methodParentCls, IntPtr method, IntPtr delegateCls, [MarshalAs(UnmanagedType.Bool)] ref bool pfIsOpenDelegate);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate CorInfoInstantiationVerification __isInstantiationOfVerifiedGeneric(IntPtr _this, IntPtr method);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __initConstraintsForVerification(IntPtr _this, IntPtr method, [MarshalAs(UnmanagedType.Bool)] ref bool pfHasCircularClassConstraints, [MarshalAs(UnmanagedType.Bool)] ref bool pfHasCircularMethodConstraint);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate CorInfoCanSkipVerificationResult __canSkipMethodVerification(IntPtr _this, IntPtr ftnHandle);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __methodMustBeLoadedBeforeCodeIsRun(IntPtr _this, IntPtr method);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __mapMethodDeclToMethodImpl(IntPtr _this, IntPtr method);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __getGSCookie(IntPtr _this, IntPtr pCookieVal, IntPtr ppCookieVal);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __resolveToken(IntPtr _this, ref CORINFO_RESOLVED_TOKEN pResolvedToken);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __tryResolveToken(IntPtr _this, ref CORINFO_RESOLVED_TOKEN pResolvedToken);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __findSig(IntPtr _this, IntPtr module, uint sigTOK, IntPtr context, IntPtr sig);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __findCallSiteSig(IntPtr _this, IntPtr module, uint methTOK, IntPtr context, IntPtr sig);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getTokenTypeAsHandle(IntPtr _this, ref CORINFO_RESOLVED_TOKEN pResolvedToken);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate CorInfoCanSkipVerificationResult __canSkipVerification(IntPtr _this, IntPtr module);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        [return: MarshalAs(UnmanagedType.Bool)] delegate bool __isValidToken(IntPtr _this, IntPtr module, uint metaTOK);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        [return: MarshalAs(UnmanagedType.Bool)] delegate bool __isValidStringRef(IntPtr _this, IntPtr module, uint metaTOK);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getStringLiteral(IntPtr _this, IntPtr module, uint metaTOK, ref int length);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        [return: MarshalAs(UnmanagedType.Bool)] delegate bool __shouldEnforceCallvirtRestriction(IntPtr _this, IntPtr scope);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate CorInfoType __asCorInfoType(IntPtr _this, IntPtr cls);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getClassName(IntPtr _this, IntPtr cls);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getClassNameFromMetadata(IntPtr _this, IntPtr cls, IntPtr namespaceName);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getTypeInstantiationArgument(IntPtr _this, IntPtr cls, uint index);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate int __appendClassName(IntPtr _this, IntPtr ppBuf, ref int pnBufLen, IntPtr cls, [MarshalAs(UnmanagedType.Bool)]bool fNamespace, [MarshalAs(UnmanagedType.Bool)]bool fFullInst, [MarshalAs(UnmanagedType.Bool)]bool fAssembly);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        [return: MarshalAs(UnmanagedType.Bool)] delegate bool __isValueClass(IntPtr _this, IntPtr cls);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate CorInfoInlineTypeCheck __canInlineTypeCheck(IntPtr _this, IntPtr cls, CorInfoInlineTypeCheckSource source);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        [return: MarshalAs(UnmanagedType.Bool)] delegate bool __canInlineTypeCheckWithObjectVTable(IntPtr _this, IntPtr cls);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate uint __getClassAttribs(IntPtr _this, IntPtr cls);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        [return: MarshalAs(UnmanagedType.Bool)] delegate bool __isStructRequiringStackAllocRetBuf(IntPtr _this, IntPtr cls);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getClassModule(IntPtr _this, IntPtr cls);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getModuleAssembly(IntPtr _this, IntPtr mod);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getAssemblyName(IntPtr _this, IntPtr assem);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __LongLifetimeMalloc(IntPtr _this, UIntPtr sz);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __LongLifetimeFree(IntPtr _this, IntPtr obj);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getClassModuleIdForStatics(IntPtr _this, IntPtr cls, IntPtr pModule, IntPtr ppIndirection);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate uint __getClassSize(IntPtr _this, IntPtr cls);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate uint __getHeapClassSize(IntPtr _this, IntPtr cls);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        [return: MarshalAs(UnmanagedType.Bool)] delegate bool __canAllocateOnStack(IntPtr _this, IntPtr cls);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate uint __getClassAlignmentRequirement(IntPtr _this, IntPtr cls, [MarshalAs(UnmanagedType.Bool)]bool fDoubleAlignHint);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate uint __getClassGClayout(IntPtr _this, IntPtr cls, IntPtr gcPtrs);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate uint __getClassNumInstanceFields(IntPtr _this, IntPtr cls);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getFieldInClass(IntPtr _this, IntPtr clsHnd, int num);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        [return: MarshalAs(UnmanagedType.Bool)] delegate bool __checkMethodModifier(IntPtr _this, IntPtr hMethod, IntPtr modifier, [MarshalAs(UnmanagedType.Bool)]bool fOptional);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate CorInfoHelpFunc __getNewHelper(IntPtr _this, ref CORINFO_RESOLVED_TOKEN pResolvedToken, IntPtr callerHandle, IntPtr pHasSideEffects);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate CorInfoHelpFunc __getNewArrHelper(IntPtr _this, IntPtr arrayCls);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate CorInfoHelpFunc __getCastingHelper(IntPtr _this, ref CORINFO_RESOLVED_TOKEN pResolvedToken, [MarshalAs(UnmanagedType.I1)]bool fThrowing);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate CorInfoHelpFunc __getSharedCCtorHelper(IntPtr _this, IntPtr clsHnd);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate CorInfoHelpFunc __getSecurityPrologHelper(IntPtr _this, IntPtr ftn);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getTypeForBox(IntPtr _this, IntPtr cls);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate CorInfoHelpFunc __getBoxHelper(IntPtr _this, IntPtr cls);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate CorInfoHelpFunc __getUnBoxHelper(IntPtr _this, IntPtr cls);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        [return: MarshalAs(UnmanagedType.I1)] delegate bool __getReadyToRunHelper(IntPtr _this, ref CORINFO_RESOLVED_TOKEN pResolvedToken, ref CORINFO_LOOKUP_KIND pGenericLookupKind, CorInfoHelpFunc id, ref CORINFO_CONST_LOOKUP pLookup);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __getReadyToRunDelegateCtorHelper(IntPtr _this, ref CORINFO_RESOLVED_TOKEN pTargetMethod, IntPtr delegateType, ref CORINFO_LOOKUP pLookup);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getHelperName(IntPtr _this, CorInfoHelpFunc helpFunc);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate CorInfoInitClassResult __initClass(IntPtr _this, IntPtr field, IntPtr method, IntPtr context, [MarshalAs(UnmanagedType.Bool)]bool speculative);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __classMustBeLoadedBeforeCodeIsRun(IntPtr _this, IntPtr cls);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getBuiltinClass(IntPtr _this, CorInfoClassId classId);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate CorInfoType __getTypeForPrimitiveValueClass(IntPtr _this, IntPtr cls);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate CorInfoType __getTypeForPrimitiveNumericClass(IntPtr _this, IntPtr cls);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        [return: MarshalAs(UnmanagedType.Bool)] delegate bool __canCast(IntPtr _this, IntPtr child, IntPtr parent);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        [return: MarshalAs(UnmanagedType.Bool)] delegate bool __areTypesEquivalent(IntPtr _this, IntPtr cls1, IntPtr cls2);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate TypeCompareState __compareTypesForCast(IntPtr _this, IntPtr fromClass, IntPtr toClass);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate TypeCompareState __compareTypesForEquality(IntPtr _this, IntPtr cls1, IntPtr cls2);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __mergeClasses(IntPtr _this, IntPtr cls1, IntPtr cls2);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        [return: MarshalAs(UnmanagedType.Bool)] delegate bool __isMoreSpecificType(IntPtr _this, IntPtr cls1, IntPtr cls2);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getParentType(IntPtr _this, IntPtr cls);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate CorInfoType __getChildType(IntPtr _this, IntPtr clsHnd, IntPtr clsRet);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        [return: MarshalAs(UnmanagedType.Bool)] delegate bool __satisfiesClassConstraints(IntPtr _this, IntPtr cls);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        [return: MarshalAs(UnmanagedType.Bool)] delegate bool __isSDArray(IntPtr _this, IntPtr cls);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate uint __getArrayRank(IntPtr _this, IntPtr cls);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getArrayInitializationData(IntPtr _this, IntPtr field, uint size);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate CorInfoIsAccessAllowedResult __canAccessClass(IntPtr _this, ref CORINFO_RESOLVED_TOKEN pResolvedToken, IntPtr callerHandle, ref CORINFO_HELPER_DESC pAccessHelper);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getFieldName(IntPtr _this, IntPtr ftn, IntPtr moduleName);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getFieldClass(IntPtr _this, IntPtr field);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate CorInfoType __getFieldType(IntPtr _this, IntPtr field, IntPtr structType, IntPtr memberParent);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate uint __getFieldOffset(IntPtr _this, IntPtr field);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        [return: MarshalAs(UnmanagedType.I1)] delegate bool __isWriteBarrierHelperRequired(IntPtr _this, IntPtr field);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __getFieldInfo(IntPtr _this, ref CORINFO_RESOLVED_TOKEN pResolvedToken, IntPtr callerHandle, CORINFO_ACCESS_FLAGS flags, IntPtr pResult);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        [return: MarshalAs(UnmanagedType.I1)] delegate bool __isFieldStatic(IntPtr _this, IntPtr fldHnd);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __getBoundaries(IntPtr _this, IntPtr ftn, ref uint cILOffsets, ref IntPtr pILOffsets, IntPtr implictBoundaries);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __setBoundaries(IntPtr _this, IntPtr ftn, uint cMap, IntPtr pMap);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __getVars(IntPtr _this, IntPtr ftn, ref uint cVars, IntPtr vars, [MarshalAs(UnmanagedType.U1)] ref bool extendOthers);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __setVars(IntPtr _this, IntPtr ftn, uint cVars, IntPtr vars);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __allocateArray(IntPtr _this, uint cBytes);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __freeArray(IntPtr _this, IntPtr array);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getArgNext(IntPtr _this, IntPtr args);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate CorInfoTypeWithMod __getArgType(IntPtr _this, IntPtr sig, IntPtr args, IntPtr vcTypeRet);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getArgClass(IntPtr _this, IntPtr sig, IntPtr args);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate CorInfoType __getHFAType(IntPtr _this, IntPtr hClass);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate HRESULT __GetErrorHRESULT(IntPtr _this, IntPtr pExceptionPointers);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate uint __GetErrorMessage(IntPtr _this, IntPtr buffer, uint bufferLength);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate int __FilterException(IntPtr _this, IntPtr pExceptionPointers);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __HandleException(IntPtr _this, IntPtr pExceptionPointers);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __ThrowExceptionForJitResult(IntPtr _this, HRESULT result);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __ThrowExceptionForHelper(IntPtr _this, ref CORINFO_HELPER_DESC throwHelper);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        [return: MarshalAs(UnmanagedType.I1)] delegate bool __runWithErrorTrap(IntPtr _this, IntPtr function, IntPtr parameter);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __getEEInfo(IntPtr _this, ref CORINFO_EE_INFO pEEInfoOut);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getJitTimeLogFilename(IntPtr _this, IntPtr ppException);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate mdToken __getMethodDefFromMethod(IntPtr _this, IntPtr hMethod);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getMethodName(IntPtr _this, IntPtr ftn, IntPtr moduleName);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getMethodNameFromMetadata(IntPtr _this, IntPtr ftn, IntPtr className, IntPtr namespaceName, IntPtr enclosingClassName);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate uint __getMethodHash(IntPtr _this, IntPtr ftn);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __findNameOfToken(IntPtr _this, IntPtr moduleHandle, mdToken token, IntPtr szFQName, UIntPtr FQNameCapacity);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        [return: MarshalAs(UnmanagedType.I1)] delegate bool __getSystemVAmd64PassStructInRegisterDescriptor(IntPtr _this, IntPtr structHnd, IntPtr structPassInRegDescPtr);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate uint __getThreadTLSIndex(IntPtr _this, ref IntPtr ppIndirection);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getInlinedCallFrameVptr(IntPtr _this, ref IntPtr ppIndirection);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getAddrOfCaptureThreadGlobal(IntPtr _this, ref IntPtr ppIndirection);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getHelperFtn(IntPtr _this, CorInfoHelpFunc ftnNum, ref IntPtr ppIndirection);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __getFunctionEntryPoint(IntPtr _this, IntPtr ftn, ref CORINFO_CONST_LOOKUP pResult, CORINFO_ACCESS_FLAGS accessFlags);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __getFunctionFixedEntryPoint(IntPtr _this, IntPtr ftn, ref CORINFO_CONST_LOOKUP pResult);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getMethodSync(IntPtr _this, IntPtr ftn, ref IntPtr ppIndirection);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate CorInfoHelpFunc __getLazyStringLiteralHelper(IntPtr _this, IntPtr handle);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __embedModuleHandle(IntPtr _this, IntPtr handle, ref IntPtr ppIndirection);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __embedClassHandle(IntPtr _this, IntPtr handle, ref IntPtr ppIndirection);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __embedMethodHandle(IntPtr _this, IntPtr handle, ref IntPtr ppIndirection);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __embedFieldHandle(IntPtr _this, IntPtr handle, ref IntPtr ppIndirection);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __embedGenericHandle(IntPtr _this, ref CORINFO_RESOLVED_TOKEN pResolvedToken, [MarshalAs(UnmanagedType.Bool)]bool fEmbedParent, ref CORINFO_GENERICHANDLE_RESULT pResult);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __getLocationOfThisType(IntPtr _this, out CORINFO_LOOKUP_KIND _return, IntPtr context);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getPInvokeUnmanagedTarget(IntPtr _this, IntPtr method, ref IntPtr ppIndirection);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getAddressOfPInvokeFixup(IntPtr _this, IntPtr method, ref IntPtr ppIndirection);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __getAddressOfPInvokeTarget(IntPtr _this, IntPtr method, ref CORINFO_CONST_LOOKUP pLookup);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __GetCookieForPInvokeCalliSig(IntPtr _this, IntPtr szMetaSig, ref IntPtr ppIndirection);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        [return: MarshalAs(UnmanagedType.I1)] delegate bool __canGetCookieForPInvokeCalliSig(IntPtr _this, IntPtr szMetaSig);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getJustMyCodeHandle(IntPtr _this, IntPtr method, ref IntPtr ppIndirection);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __GetProfilingHandle(IntPtr _this, [MarshalAs(UnmanagedType.Bool)] ref bool pbHookFunction, ref IntPtr pProfilerHandle, [MarshalAs(UnmanagedType.Bool)] ref bool pbIndirectedHandles);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __getCallInfo(IntPtr _this, ref CORINFO_RESOLVED_TOKEN pResolvedToken, IntPtr pConstrainedResolvedToken, IntPtr callerHandle, CORINFO_CALLINFO_FLAGS flags, IntPtr pResult);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        [return: MarshalAs(UnmanagedType.Bool)] delegate bool __canAccessFamily(IntPtr _this, IntPtr hCaller, IntPtr hInstanceType);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        [return: MarshalAs(UnmanagedType.Bool)] delegate bool __isRIDClassDomainID(IntPtr _this, IntPtr cls);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate uint __getClassDomainID(IntPtr _this, IntPtr cls, ref IntPtr ppIndirection);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getFieldAddress(IntPtr _this, IntPtr field, IntPtr ppIndirection);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getStaticFieldCurrentClass(IntPtr _this, IntPtr field, IntPtr pIsSpeculative);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getVarArgsHandle(IntPtr _this, IntPtr pSig, ref IntPtr ppIndirection);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        [return: MarshalAs(UnmanagedType.I1)] delegate bool __canGetVarArgsHandle(IntPtr _this, IntPtr pSig);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate InfoAccessType __constructStringLiteral(IntPtr _this, IntPtr module, mdToken metaTok, ref IntPtr ppValue);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate InfoAccessType __emptyStringLiteral(IntPtr _this, ref IntPtr ppValue);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate uint __getFieldThreadLocalStoreID(IntPtr _this, IntPtr field, ref IntPtr ppIndirection);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __setOverride(IntPtr _this, IntPtr pOverride, IntPtr currentMethod);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __addActiveDependency(IntPtr _this, IntPtr moduleFrom, IntPtr moduleTo);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __GetDelegateCtor(IntPtr _this, IntPtr methHnd, IntPtr clsHnd, IntPtr targetMethodHnd, ref DelegateCtorArgs pCtorData);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __MethodCompileComplete(IntPtr _this, IntPtr methHnd);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getTailCallCopyArgsThunk(IntPtr _this, IntPtr pSig, CorInfoHelperTailCallSpecialHandling flags);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        [return: MarshalAs(UnmanagedType.I1)] delegate bool __convertPInvokeCalliToCall(IntPtr _this, ref CORINFO_RESOLVED_TOKEN pResolvedToken, [MarshalAs(UnmanagedType.I1)]bool mustConvert);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __getMemoryManager(IntPtr _this, IntPtr ppException);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __allocMem(IntPtr _this, uint hotCodeSize, uint coldCodeSize, uint roDataSize, uint xcptnsCount, CorJitAllocMemFlag flag, ref IntPtr hotCodeBlock, ref IntPtr coldCodeBlock, ref IntPtr roDataBlock);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __reserveUnwindInfo(IntPtr _this, [MarshalAs(UnmanagedType.Bool)]bool isFunclet, [MarshalAs(UnmanagedType.Bool)]bool isColdCode, uint unwindSize);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __allocUnwindInfo(IntPtr _this, IntPtr pHotCode, IntPtr pColdCode, uint startOffset, uint endOffset, uint unwindSize, IntPtr pUnwindBlock, CorJitFuncKind funcKind);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate IntPtr __allocGCInfo(IntPtr _this, UIntPtr size);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __yieldExecution(IntPtr _this, IntPtr ppException);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __setEHcount(IntPtr _this, uint cEH);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __setEHinfo(IntPtr _this, uint EHnumber, ref CORINFO_EH_CLAUSE clause);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        [return: MarshalAs(UnmanagedType.Bool)] delegate bool __logMsg(IntPtr _this, uint level, IntPtr fmt, IntPtr args);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate int __doAssert(IntPtr _this, IntPtr szFile, int iLine, IntPtr szExpr);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __reportFatalError(IntPtr _this, CorJitResult result);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate HRESULT __allocMethodBlockCounts(IntPtr _this, uint count, ref IntPtr pBlockCounts);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate HRESULT __getMethodBlockCounts(IntPtr _this, IntPtr ftnHnd, ref uint pCount, ref IntPtr pBlockCounts, ref uint pNumRuns);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __recordCallSite(IntPtr _this, uint instrOffset, IntPtr callSig, IntPtr methodHandle);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __recordRelocation(IntPtr _this, IntPtr location, IntPtr target, ushort fRelocType, ushort slotNum, int addlDelta);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate ushort __getRelocTypeHint(IntPtr _this, IntPtr target);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate void __getModuleNativeEntryPointRange(IntPtr _this, ref IntPtr pStart, ref IntPtr pEnd);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate uint __getExpectedTargetArchitecture(IntPtr _this, IntPtr ppException);
        [UnmanagedFunctionPointerAttribute(default(CallingConvention))]
        public delegate uint __getJitFlags(IntPtr _this, ref CORJIT_FLAGS flags, uint sizeInBytes);
    }
}
