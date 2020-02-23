using System;
using System.Runtime.InteropServices;
namespace CoreRT.JitInterface
{
    public struct CorInfoImpl
    {
        [UnmanagedFunctionPointer(default)]
        public delegate uint GetMethodAttribsDelegate(IntPtr thisHandle, IntPtr ftn);
        public GetMethodAttribsDelegate GetMethodAttribs { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void SetMethodAttribsDelegate(IntPtr thisHandle, IntPtr ftn, CorInfoMethodRuntimeFlags attribs);
        public SetMethodAttribsDelegate SetMethodAttribs { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void GetMethodSigDelegate(IntPtr thisHandle, IntPtr ftn, IntPtr sig, IntPtr memberParent);
        public GetMethodSigDelegate GetMethodSig { get; }

        [UnmanagedFunctionPointer(default)]
        [return: MarshalAs(UnmanagedType.I1)]
        public delegate bool GetMethodInfoDelegate(IntPtr thisHandle, IntPtr ftn, IntPtr info);
        public GetMethodInfoDelegate GetMethodInfo { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate CorInfoInline CanInlineDelegate(IntPtr thisHandle, IntPtr callerHnd, IntPtr calleeHnd, ref uint pRestrictions);
        public CanInlineDelegate CanInline { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void ReportInliningDecisionDelegate(IntPtr thisHandle, IntPtr inlinerHnd, IntPtr inlineeHnd, CorInfoInline inlineResult, IntPtr reason);
        public ReportInliningDecisionDelegate ReportInliningDecision { get; }

        [UnmanagedFunctionPointer(default)]
        [return: MarshalAs(UnmanagedType.I1)]
        public delegate bool CanTailCallDelegate(IntPtr thisHandle, IntPtr callerHnd, IntPtr declaredCalleeHnd, IntPtr exactCalleeHnd, [MarshalAs(UnmanagedType.I1)] bool fIsTailPrefix);
        public CanTailCallDelegate CanTailCall { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void ReportTailCallDecisionDelegate(IntPtr thisHandle, IntPtr callerHnd, IntPtr calleeHnd, [MarshalAs(UnmanagedType.I1)] bool fIsTailPrefix, CorInfoTailCall tailCallResult, IntPtr reason);
        public ReportTailCallDecisionDelegate ReportTailCallDecision { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void GetEHinfoDelegate(IntPtr thisHandle, IntPtr ftn, uint eHnumber, ref CORINFO_EH_CLAUSE clause);
        public GetEHinfoDelegate GetEHinfo { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetMethodClassDelegate(IntPtr thisHandle, IntPtr method);
        public GetMethodClassDelegate GetMethodClass { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetMethodModuleDelegate(IntPtr thisHandle, IntPtr method);
        public GetMethodModuleDelegate GetMethodModule { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void GetMethodVTableOffsetDelegate(IntPtr thisHandle, IntPtr method, ref uint offsetOfIndirection, ref uint offsetAfterIndirection, [MarshalAs(UnmanagedType.U1)] ref bool isRelative);
        public GetMethodVTableOffsetDelegate GetMethodVTableOffset { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr ResolveVirtualMethodDelegate(IntPtr thisHandle, IntPtr virtualMethod, IntPtr implementingClass, IntPtr ownerType);
        public ResolveVirtualMethodDelegate ResolveVirtualMethod { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetUnboxedEntryDelegate(IntPtr thisHandle, IntPtr ftn, IntPtr requiresInstMethodTableArg);
        public GetUnboxedEntryDelegate GetUnboxedEntry { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetDefaultEqualityComparerClassDelegate(IntPtr thisHandle, IntPtr elemType);
        public GetDefaultEqualityComparerClassDelegate GetDefaultEqualityComparerClass { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void ExpandRawHandleIntrinsicDelegate(IntPtr thisHandle, ref CORINFO_RESOLVED_TOKEN pResolvedToken, ref CORINFO_GENERICHANDLE_RESULT pResult);
        public ExpandRawHandleIntrinsicDelegate ExpandRawHandleIntrinsic { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate CorInfoIntrinsics GetIntrinsicIdDelegate(IntPtr thisHandle, IntPtr method, IntPtr pMustExpand);
        public GetIntrinsicIdDelegate GetIntrinsicId { get; }


        [UnmanagedFunctionPointer(default)]
        public delegate bool IsIntrinsicTypeDelegate(IntPtr thisHandle, IntPtr classHnd);
        public bool IsInSIMDModule { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate CorInfoUnmanagedCallConv GetUnmanagedCallConvDelegate(IntPtr thisHandle, IntPtr method);
        public GetUnmanagedCallConvDelegate GetUnmanagedCallConv { get; }

        [UnmanagedFunctionPointer(default)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool PInvokeMarshalingRequiredDelegate(IntPtr thisHandle, IntPtr method, IntPtr callSiteSig);
        public PInvokeMarshalingRequiredDelegate PInvokeMarshalingRequired { get; }

        [UnmanagedFunctionPointer(default)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool SatisfiesMethodConstraintsDelegate(IntPtr thisHandle, IntPtr parent, IntPtr method);
        public SatisfiesMethodConstraintsDelegate SatisfiesMethodConstraints { get; }

        [UnmanagedFunctionPointer(default)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool IsCompatibleDelegateDelegate(IntPtr thisHandle, IntPtr objCls, IntPtr methodParentCls, IntPtr method, IntPtr delegateCls, [MarshalAs(UnmanagedType.Bool)] ref bool pfIsOpenDelegate);
        public IsCompatibleDelegateDelegate IsCompatibleDelegate { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate CorInfoInstantiationVerification IsInstantiationOfVerifiedGenericDelegate(IntPtr thisHandle, IntPtr method);
        public IsInstantiationOfVerifiedGenericDelegate IsInstantiationOfVerifiedGeneric { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void InitConstraintsForVerificationDelegate(IntPtr thisHandle, IntPtr method, [MarshalAs(UnmanagedType.Bool)] ref bool pfHasCircularClassConstraints, [MarshalAs(UnmanagedType.Bool)] ref bool pfHasCircularMethodConstraint);
        public InitConstraintsForVerificationDelegate InitConstraintsForVerification { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate CorInfoCanSkipVerificationResult CanSkipMethodVerificationDelegate(IntPtr thisHandle, IntPtr ftnHandle);
        public CanSkipMethodVerificationDelegate CanSkipMethodVerification { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void MethodMustBeLoadedBeforeCodeIsRunDelegate(IntPtr thisHandle, IntPtr method);
        public MethodMustBeLoadedBeforeCodeIsRunDelegate MethodMustBeLoadedBeforeCodeIsRun { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr MapMethodDeclToMethodImplDelegate(IntPtr thisHandle, IntPtr method);
        public MapMethodDeclToMethodImplDelegate MapMethodDeclToMethodImpl { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void GetGsCookieDelegate(IntPtr thisHandle, IntPtr pCookieVal, IntPtr ppCookieVal);
        public GetGsCookieDelegate GetGsCookie { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void ResolveTokenDelegate(IntPtr thisHandle, ref CORINFO_RESOLVED_TOKEN pResolvedToken);
        public ResolveTokenDelegate ResolveToken { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void TryResolveTokenDelegate(IntPtr thisHandle, ref CORINFO_RESOLVED_TOKEN pResolvedToken);
        public TryResolveTokenDelegate TryResolveToken { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void FindSigDelegate(IntPtr thisHandle, IntPtr module, uint sigTok, IntPtr context, IntPtr sig);
        public FindSigDelegate FindSig { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void FindCallSiteSigDelegate(IntPtr thisHandle, IntPtr module, uint methTok, IntPtr context, IntPtr sig);
        public FindCallSiteSigDelegate FindCallSiteSig { get; }

        [UnmanagedFunctionPointerAttribute(default)]
        public delegate IntPtr GetTokenTypeAsHandleDelegate(IntPtr thisHandle, ref CORINFO_RESOLVED_TOKEN pResolvedToken);
        public GetTokenTypeAsHandleDelegate GetTokenTypeAsHandle { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate CorInfoCanSkipVerificationResult CanSkipVerificationDelegate(IntPtr thisHandle, IntPtr module);
        public CanSkipVerificationDelegate CanSkipVerification { get; }

        [UnmanagedFunctionPointer(default)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool IsValidTokenDelegate(IntPtr thisHandle, IntPtr module, uint metaTok);
        public IsValidTokenDelegate IsValidToken { get; }

        [UnmanagedFunctionPointer(default)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool IsValidStringRefDelegate(IntPtr thisHandle, IntPtr module, uint metaTok);
        public IsValidStringRefDelegate IsValidStringRef { get; }

        [UnmanagedFunctionPointer(default)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool ShouldEnforceCallvirtRestrictionDelegate(IntPtr thisHandle, IntPtr scope);
        public ShouldEnforceCallvirtRestrictionDelegate ShouldEnforceCallvirtRestriction { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate CorInfoType AsCorInfoTypeDelegate(IntPtr thisHandle, IntPtr cls);
        public AsCorInfoTypeDelegate AsCorInfoType { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetClassNameDelegate(IntPtr thisHandle, IntPtr cls);
        public GetClassNameDelegate GetClassName { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetClassNameFromMetadataDelegate(IntPtr thisHandle, IntPtr cls, IntPtr namespaceName);
        public GetClassNameFromMetadataDelegate GetClassNameFromMetadata { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetTypeInstantiationArgumentDelegate(IntPtr thisHandle, IntPtr cls, uint index);
        public GetTypeInstantiationArgumentDelegate GetTypeInstantiationArgument { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate int AppendClassNameDelegate(IntPtr thisHandle, IntPtr ppBuf, ref int pnBufLen, IntPtr cls, [MarshalAs(UnmanagedType.Bool)] bool fNamespace, [MarshalAs(UnmanagedType.Bool)] bool fFullInst, [MarshalAs(UnmanagedType.Bool)] bool fAssembly);
        public AppendClassNameDelegate AppendClassName { get; }

        [UnmanagedFunctionPointer(default)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool IsValueClassDelegate(IntPtr thisHandle, IntPtr cls);
        public IsValueClassDelegate IsValueClass { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate CorInfoInlineTypeCheck CanInlineTypeCheckDelegate(IntPtr thisHandle, IntPtr cls, CorInfoInlineTypeCheckSource source);
        public CanInlineTypeCheckDelegate CanInlineTypeCheck { get; }

        [UnmanagedFunctionPointer(default)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool CanInlineTypeCheckWithObjectVTableDelegate(IntPtr thisHandle, IntPtr cls);
        public CanInlineTypeCheckWithObjectVTableDelegate CanInlineTypeCheckWithObjectVTable { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate uint GetClassAttribsDelegate(IntPtr thisHandle, IntPtr cls);
        public GetClassAttribsDelegate GetClassAttribs { get; }

        [UnmanagedFunctionPointer(default)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool IsStructRequiringStackAllocRetBufDelegate(IntPtr thisHandle, IntPtr cls);
        public IsStructRequiringStackAllocRetBufDelegate IsStructRequiringStackAllocRetBuf { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetClassModuleDelegate(IntPtr thisHandle, IntPtr cls);
        public GetClassModuleDelegate GetClassModule { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetModuleAssemblyDelegate(IntPtr thisHandle, IntPtr mod);
        public GetModuleAssemblyDelegate GetModuleAssembly { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetAssemblyNameDelegate(IntPtr thisHandle, IntPtr assem);
        public GetAssemblyNameDelegate GetAssemblyName { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr LongLifetimeMallocDelegate(IntPtr thisHandle, UIntPtr sz);
        public LongLifetimeMallocDelegate LongLifetimeMalloc { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void LongLifetimeFreeDelegate(IntPtr thisHandle, IntPtr obj);
        public LongLifetimeFreeDelegate LongLifetimeFree { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetClassModuleIdForStaticsDelegate(IntPtr thisHandle, IntPtr cls, IntPtr pModule, IntPtr ppIndirection);
        public GetClassModuleIdForStaticsDelegate GetClassModuleIdForStatics { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate uint GetClassSizeDelegate(IntPtr thisHandle, IntPtr cls);
        public GetClassSizeDelegate GetClassSize { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate uint GetHeapClassSizeDelegate(IntPtr thisHandle, IntPtr cls);
        public GetHeapClassSizeDelegate GetHeapClassSize { get; }

        [UnmanagedFunctionPointer(default)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool CanAllocateOnStackDelegate(IntPtr thisHandle, IntPtr cls);
        public CanAllocateOnStackDelegate CanAllocateOnStack { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate uint GetClassAlignmentRequirementDelegate(IntPtr thisHandle, IntPtr cls, [MarshalAs(UnmanagedType.Bool)] bool fDoubleAlignHint);
        public GetClassAlignmentRequirementDelegate GetClassAlignmentRequirement { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate uint GetClassGClayoutDelegate(IntPtr thisHandle, IntPtr cls, IntPtr gcPtrs);
        public GetClassGClayoutDelegate GetClassGClayout { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate uint GetClassNumInstanceFieldsDelegate(IntPtr thisHandle, IntPtr cls);
        public GetClassNumInstanceFieldsDelegate GetClassNumInstanceFields { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetFieldInClassDelegate(IntPtr thisHandle, IntPtr clsHnd, int num);
        public GetFieldInClassDelegate GetFieldInClass { get; }

        [UnmanagedFunctionPointer(default)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool CheckMethodModifierDelegate(IntPtr thisHandle, IntPtr hMethod, IntPtr modifier, [MarshalAs(UnmanagedType.Bool)] bool fOptional);
        public CheckMethodModifierDelegate CheckMethodModifier { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate CorInfoHelpFunc GetNewHelperDelegate(IntPtr thisHandle, ref CORINFO_RESOLVED_TOKEN pResolvedToken, IntPtr callerHandle, IntPtr pHasSideEffects);
        public GetNewHelperDelegate GetNewHelper { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate CorInfoHelpFunc GetNewArrHelperDelegate(IntPtr thisHandle, IntPtr arrayCls);
        public GetNewArrHelperDelegate GetNewArrHelper { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate CorInfoHelpFunc GetCastingHelperDelegate(IntPtr thisHandle, ref CORINFO_RESOLVED_TOKEN pResolvedToken, [MarshalAs(UnmanagedType.I1)] bool fThrowing);
        public GetCastingHelperDelegate GetCastingHelper { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate CorInfoHelpFunc GetSharedCCtorHelperDelegate(IntPtr thisHandle, IntPtr clsHnd);
        public GetSharedCCtorHelperDelegate GetSharedCCtorHelper { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate CorInfoHelpFunc GetSecurityPrologHelperDelegate(IntPtr thisHandle, IntPtr ftn);
        public GetSecurityPrologHelperDelegate GetSecurityPrologHelper { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetTypeForBoxDelegate(IntPtr thisHandle, IntPtr cls);
        public GetTypeForBoxDelegate GetTypeForBox { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate CorInfoHelpFunc GetBoxHelperDelegate(IntPtr thisHandle, IntPtr cls);
        public GetBoxHelperDelegate GetBoxHelper { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate CorInfoHelpFunc GetUnBoxHelperDelegate(IntPtr thisHandle, IntPtr cls);
        public GetUnBoxHelperDelegate GetUnBoxHelper { get; }

        [UnmanagedFunctionPointer(default)]
        [return: MarshalAs(UnmanagedType.I1)]
        public delegate bool GetReadyToRunHelperDelegate(IntPtr thisHandle, ref CORINFO_RESOLVED_TOKEN pResolvedToken, ref CORINFO_LOOKUP_KIND pGenericLookupKind, CorInfoHelpFunc id, ref CORINFO_CONST_LOOKUP pLookup);
        public GetReadyToRunHelperDelegate GetReadyToRunHelper { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void GetReadyToRunDelegateCtorHelperDelegate(IntPtr thisHandle, ref CORINFO_RESOLVED_TOKEN pTargetMethod, IntPtr delegateType, ref CORINFO_LOOKUP pLookup);
        public GetReadyToRunDelegateCtorHelperDelegate GetReadyToRunDelegateCtorHelper { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetHelperNameDelegate(IntPtr thisHandle, CorInfoHelpFunc helpFunc);
        public GetHelperNameDelegate GetHelperName { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate CorInfoInitClassResult InitClassDelegate(IntPtr thisHandle, IntPtr field, IntPtr method, IntPtr context, [MarshalAs(UnmanagedType.Bool)] bool speculative);
        public InitClassDelegate InitClass { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void ClassMustBeLoadedBeforeCodeIsRunDelegate(IntPtr thisHandle, IntPtr cls);
        public ClassMustBeLoadedBeforeCodeIsRunDelegate ClassMustBeLoadedBeforeCodeIsRun { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetBuiltinClassDelegate(IntPtr thisHandle, CorInfoClassId classId);
        public GetBuiltinClassDelegate GetBuiltinClass { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate CorInfoType GetTypeForPrimitiveValueClassDelegate(IntPtr thisHandle, IntPtr cls);
        public GetTypeForPrimitiveValueClassDelegate GetTypeForPrimitiveValueClass { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate CorInfoType GetTypeForPrimitiveNumericClassDelegate(IntPtr thisHandle, IntPtr cls);
        public GetTypeForPrimitiveNumericClassDelegate GetTypeForPrimitiveNumericClass { get; }

        [UnmanagedFunctionPointer(default)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool CanCastDelegate(IntPtr thisHandle, IntPtr child, IntPtr parent);
        public CanCastDelegate CanCast { get; }

        [UnmanagedFunctionPointer(default)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool AreTypesEquivalentDelegate(IntPtr thisHandle, IntPtr cls1, IntPtr cls2);
        public AreTypesEquivalentDelegate AreTypesEquivalent { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate TypeCompareState CompareTypesForCastDelegate(IntPtr thisHandle, IntPtr fromClass, IntPtr toClass);
        public CompareTypesForCastDelegate CompareTypesForCast { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate TypeCompareState CompareTypesForEqualityDelegate(IntPtr thisHandle, IntPtr cls1, IntPtr cls2);
        public CompareTypesForEqualityDelegate CompareTypesForEquality { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr MergeClassesDelegate(IntPtr thisHandle, IntPtr cls1, IntPtr cls2);
        public MergeClassesDelegate MergeClasses { get; }

        [UnmanagedFunctionPointer(default)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool IsMoreSpecificTypeDelegate(IntPtr thisHandle, IntPtr cls1, IntPtr cls2);
        public IsMoreSpecificTypeDelegate IsMoreSpecificType { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetParentTypeDelegate(IntPtr thisHandle, IntPtr cls);
        public GetParentTypeDelegate GetParentType { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate CorInfoType GetChildTypeDelegate(IntPtr thisHandle, IntPtr clsHnd, IntPtr clsRet);
        public GetChildTypeDelegate GetChildType { get; }

        [UnmanagedFunctionPointer(default)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool SatisfiesClassConstraintsDelegate(IntPtr thisHandle, IntPtr cls);
        public SatisfiesClassConstraintsDelegate SatisfiesClassConstraints { get; }

        [UnmanagedFunctionPointer(default)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool IsSdArrayDelegate(IntPtr thisHandle, IntPtr cls);
        public IsSdArrayDelegate IsSdArray { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate uint GetArrayRankDelegate(IntPtr thisHandle, IntPtr cls);
        public GetArrayRankDelegate GetArrayRank { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetArrayInitializationDataDelegate(IntPtr thisHandle, IntPtr field, uint size);
        public GetArrayInitializationDataDelegate GetArrayInitializationData { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate CorInfoIsAccessAllowedResult CanAccessClassDelegate(IntPtr thisHandle, ref CORINFO_RESOLVED_TOKEN pResolvedToken, IntPtr callerHandle, ref CORINFO_HELPER_DESC pAccessHelper);
        public CanAccessClassDelegate CanAccessClass { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetFieldNameDelegate(IntPtr thisHandle, IntPtr ftn, IntPtr moduleName);
        public GetFieldNameDelegate GetFieldName { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetFieldClassDelegate(IntPtr thisHandle, IntPtr field);
        public GetFieldClassDelegate GetFieldClass { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate CorInfoType GetFieldTypeDelegate(IntPtr thisHandle, IntPtr field, IntPtr structType, IntPtr memberParent);
        public GetFieldTypeDelegate GetFieldType { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate uint GetFieldOffsetDelegate(IntPtr thisHandle, IntPtr field);
        public GetFieldOffsetDelegate GetFieldOffset { get; }

        [UnmanagedFunctionPointer(default)]
        [return: MarshalAs(UnmanagedType.I1)]
        public delegate bool IsWriteBarrierHelperRequiredDelegate(IntPtr thisHandle, IntPtr field);
        public IsWriteBarrierHelperRequiredDelegate IsWriteBarrierHelperRequired { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void GetFieldInfoDelegate(IntPtr thisHandle, ref CORINFO_RESOLVED_TOKEN pResolvedToken, IntPtr callerHandle, CORINFO_ACCESS_FLAGS flags, IntPtr pResult);
        public GetFieldInfoDelegate GetFieldInfo { get; }

        [UnmanagedFunctionPointer(default)]
        [return: MarshalAs(UnmanagedType.I1)]
        public delegate bool IsFieldStaticDelegate(IntPtr thisHandle, IntPtr fldHnd);
        public IsFieldStaticDelegate IsFieldStatic { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void GetBoundariesDelegate(IntPtr thisHandle, IntPtr ftn, ref uint cIlOffsets, ref IntPtr pIlOffsets, IntPtr implictBoundaries);
        public GetBoundariesDelegate GetBoundaries { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void SetBoundariesDelegate(IntPtr thisHandle, IntPtr ftn, uint cMap, IntPtr pMap);
        public SetBoundariesDelegate SetBoundaries { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void GetVarsDelegate(IntPtr thisHandle, IntPtr ftn, ref uint cVars, IntPtr vars, [MarshalAs(UnmanagedType.U1)] ref bool extendOthers);
        public GetVarsDelegate GetVars { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void SetVarsDelegate(IntPtr thisHandle, IntPtr ftn, uint cVars, IntPtr vars);
        public SetVarsDelegate SetVars { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr AllocateArrayDelegate(IntPtr thisHandle, uint cBytes);
        public AllocateArrayDelegate AllocateArray { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void FreeArrayDelegate(IntPtr thisHandle, IntPtr array);
        public FreeArrayDelegate FreeArray { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetArgNextDelegate(IntPtr thisHandle, IntPtr args);
        public GetArgNextDelegate GetArgNext { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate CorInfoTypeWithMod GetArgTypeDelegate(IntPtr thisHandle, IntPtr sig, IntPtr args, IntPtr vcTypeRet);
        public GetArgTypeDelegate GetArgType { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetArgClassDelegate(IntPtr thisHandle, IntPtr sig, IntPtr args);
        public GetArgClassDelegate GetArgClass { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate CorInfoType GetHfaTypeDelegate(IntPtr thisHandle, IntPtr hClass);
        public GetHfaTypeDelegate GetHfaType { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate HRESULT GetErrorHresultDelegate(IntPtr thisHandle, IntPtr pExceptionPointers);
        public GetErrorHresultDelegate GetErrorHresult { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate uint GetErrorMessageDelegate(IntPtr thisHandle, IntPtr buffer, uint bufferLength);
        public GetErrorMessageDelegate GetErrorMessage { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate int FilterExceptionDelegate(IntPtr thisHandle, IntPtr pExceptionPointers);
        public FilterExceptionDelegate FilterException { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void HandleExceptionDelegate(IntPtr thisHandle, IntPtr pExceptionPointers);
        public HandleExceptionDelegate HandleException { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void ThrowExceptionForJitResultDelegate(IntPtr thisHandle, HRESULT result);
        public ThrowExceptionForJitResultDelegate ThrowExceptionForJitResult { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void ThrowExceptionForHelperDelegate(IntPtr thisHandle, ref CORINFO_HELPER_DESC throwHelper);
        public ThrowExceptionForHelperDelegate ThrowExceptionForHelper { get; }

        [UnmanagedFunctionPointer(default)]
        [return: MarshalAs(UnmanagedType.I1)]
        public delegate bool RunWithErrorTrapDelegate(IntPtr thisHandle, IntPtr function, IntPtr parameter);
        public RunWithErrorTrapDelegate RunWithErrorTrap { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void GetEeInfoDelegate(IntPtr thisHandle, ref CORINFO_EE_INFO pEeInfoOut);
        public GetEeInfoDelegate GetEeInfo { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetJitTimeLogFilenameDelegate(IntPtr thisHandle, IntPtr ppException);
        public GetJitTimeLogFilenameDelegate GetJitTimeLogFilename { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate uint GetMethodDefFromMethodDelegate(IntPtr thisHandle, IntPtr hMethod);
        public GetMethodDefFromMethodDelegate GetMethodDefFromMethod { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetMethodNameDelegate(IntPtr thisHandle, IntPtr ftn, IntPtr moduleName);
        public GetMethodNameDelegate GetMethodName { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetMethodNameFromMetadataDelegate(IntPtr thisHandle, IntPtr ftn, IntPtr className, IntPtr namespaceName, IntPtr enclosingClassName);
        public GetMethodNameFromMetadataDelegate GetMethodNameFromMetadata { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate uint GetMethodHashDelegate(IntPtr thisHandle, IntPtr ftn);
        public GetMethodHashDelegate GetMethodHash { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr FindNameOfTokenDelegate(IntPtr thisHandle, IntPtr moduleHandle, uint token, IntPtr szFqName, UIntPtr fqNameCapacity);
        public FindNameOfTokenDelegate FindNameOfToken { get; }

        [UnmanagedFunctionPointer(default)]
        [return: MarshalAs(UnmanagedType.I1)]
        public delegate bool GetSystemVAmd64PassStructInRegisterDescriptorDelegate(IntPtr thisHandle, IntPtr structHnd, IntPtr structPassInRegDescPtr);
        public GetSystemVAmd64PassStructInRegisterDescriptorDelegate GetSystemVAmd64PassStructInRegisterDescriptor { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate uint GetThreadTlsIndexDelegate(IntPtr thisHandle, ref IntPtr ppIndirection);
        public GetThreadTlsIndexDelegate GetThreadTlsIndex { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetInlinedCallFrameVptrDelegate(IntPtr thisHandle, ref IntPtr ppIndirection);
        public GetInlinedCallFrameVptrDelegate GetInlinedCallFrameVptr { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetAddrOfCaptureThreadGlobalDelegate(IntPtr thisHandle, ref IntPtr ppIndirection);
        public GetAddrOfCaptureThreadGlobalDelegate GetAddrOfCaptureThreadGlobal { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetHelperFtnDelegate(IntPtr thisHandle, CorInfoHelpFunc ftnNum, ref IntPtr ppIndirection);
        public GetHelperFtnDelegate GetHelperFtn { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void GetFunctionEntryPointDelegate(IntPtr thisHandle, IntPtr ftn, ref CORINFO_CONST_LOOKUP pResult, CORINFO_ACCESS_FLAGS accessFlags);
        public GetFunctionEntryPointDelegate GetFunctionEntryPoint { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void GetFunctionFixedEntryPointDelegate(IntPtr thisHandle, IntPtr ftn, ref CORINFO_CONST_LOOKUP pResult);
        public GetFunctionFixedEntryPointDelegate GetFunctionFixedEntryPoint { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetMethodSyncDelegate(IntPtr thisHandle, IntPtr ftn, ref IntPtr ppIndirection);
        public GetMethodSyncDelegate GetMethodSync { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate CorInfoHelpFunc GetLazyStringLiteralHelperDelegate(IntPtr thisHandle, IntPtr handle);
        public GetLazyStringLiteralHelperDelegate GetLazyStringLiteralHelper { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr EmbedModuleHandleDelegate(IntPtr thisHandle, IntPtr handle, ref IntPtr ppIndirection);
        public EmbedModuleHandleDelegate EmbedModuleHandle { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr EmbedClassHandleDelegate(IntPtr thisHandle, IntPtr handle, ref IntPtr ppIndirection);
        public EmbedClassHandleDelegate EmbedClassHandle { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr EmbedMethodHandleDelegate(IntPtr thisHandle, IntPtr handle, ref IntPtr ppIndirection);
        public EmbedMethodHandleDelegate EmbedMethodHandle { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr EmbedFieldHandleDelegate(IntPtr thisHandle, IntPtr handle, ref IntPtr ppIndirection);
        public EmbedFieldHandleDelegate EmbedFieldHandle { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void EmbedGenericHandleDelegate(IntPtr thisHandle, ref CORINFO_RESOLVED_TOKEN pResolvedToken, [MarshalAs(UnmanagedType.Bool)] bool fEmbedParent, ref CORINFO_GENERICHANDLE_RESULT pResult);
        public EmbedGenericHandleDelegate EmbedGenericHandle { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void GetLocationOfThisTypeDelegate(IntPtr thisHandle, out CORINFO_LOOKUP_KIND @return, IntPtr context);
        public GetLocationOfThisTypeDelegate GetLocationOfThisType { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetPInvokeUnmanagedTargetDelegate(IntPtr thisHandle, IntPtr method, ref IntPtr ppIndirection);
        public GetPInvokeUnmanagedTargetDelegate GetPInvokeUnmanagedTarget { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetAddressOfPInvokeFixupDelegate(IntPtr thisHandle, IntPtr method, ref IntPtr ppIndirection);
        public GetAddressOfPInvokeFixupDelegate GetAddressOfPInvokeFixup { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void GetAddressOfPInvokeTargetDelegate(IntPtr thisHandle, IntPtr method, ref CORINFO_CONST_LOOKUP pLookup);
        public GetAddressOfPInvokeTargetDelegate GetAddressOfPInvokeTarget { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetCookieForPInvokeCalliSigDelegate(IntPtr thisHandle, IntPtr szMetaSig, ref IntPtr ppIndirection);
        public GetCookieForPInvokeCalliSigDelegate GetCookieForPInvokeCalliSig { get; }

        [UnmanagedFunctionPointer(default)]
        [return: MarshalAs(UnmanagedType.I1)]
        public delegate bool CanGetCookieForPInvokeCalliSigDelegate(IntPtr thisHandle, IntPtr szMetaSig);
        public CanGetCookieForPInvokeCalliSigDelegate CanGetCookieForPInvokeCalliSig { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetJustMyCodeHandleDelegate(IntPtr thisHandle, IntPtr method, ref IntPtr ppIndirection);
        public GetJustMyCodeHandleDelegate GetJustMyCodeHandle { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void GetProfilingHandleDelegate(IntPtr thisHandle, [MarshalAs(UnmanagedType.Bool)] ref bool pbHookFunction, ref IntPtr pProfilerHandle, [MarshalAs(UnmanagedType.Bool)] ref bool pbIndirectedHandles);
        public GetProfilingHandleDelegate GetProfilingHandle { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void GetCallInfoDelegate(IntPtr thisHandle, ref CORINFO_RESOLVED_TOKEN pResolvedToken, IntPtr pConstrainedResolvedToken, IntPtr callerHandle, CORINFO_CALLINFO_FLAGS flags, IntPtr pResult);
        public GetCallInfoDelegate GetCallInfo { get; }

        [UnmanagedFunctionPointer(default)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool CanAccessFamilyDelegate(IntPtr thisHandle, IntPtr hCaller, IntPtr hInstanceType);
        public CanAccessFamilyDelegate CanAccessFamily { get; }

        [UnmanagedFunctionPointer(default)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool IsRidClassDomainIdDelegate(IntPtr thisHandle, IntPtr cls);
        public IsRidClassDomainIdDelegate IsRidClassDomainId { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate uint GetClassDomainIdDelegate(IntPtr thisHandle, IntPtr cls, ref IntPtr ppIndirection);
        public GetClassDomainIdDelegate GetClassDomainId { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetFieldAddressDelegate(IntPtr thisHandle, IntPtr field, IntPtr ppIndirection);
        public GetFieldAddressDelegate GetFieldAddress { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetStaticFieldCurrentClassDelegate(IntPtr thisHandle, IntPtr field, IntPtr pIsSpeculative);
        public GetStaticFieldCurrentClassDelegate GetStaticFieldCurrentClass { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetVarArgsHandleDelegate(IntPtr thisHandle, IntPtr pSig, ref IntPtr ppIndirection);
        public GetVarArgsHandleDelegate GetVarArgsHandle { get; }

        [UnmanagedFunctionPointer(default)]
        [return: MarshalAs(UnmanagedType.I1)]
        public delegate bool CanGetVarArgsHandleDelegate(IntPtr thisHandle, IntPtr pSig);
        public CanGetVarArgsHandleDelegate CanGetVarArgsHandle { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate InfoAccessType ConstructStringLiteralDelegate(IntPtr thisHandle, IntPtr module, uint metaTok, ref IntPtr ppValue);
        public ConstructStringLiteralDelegate ConstructStringLiteral { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate InfoAccessType EmptyStringLiteralDelegate(IntPtr thisHandle, ref IntPtr ppValue);
        public EmptyStringLiteralDelegate EmptyStringLiteral { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate uint GetFieldThreadLocalStoreIdDelegate(IntPtr thisHandle, IntPtr field, ref IntPtr ppIndirection);
        public GetFieldThreadLocalStoreIdDelegate GetFieldThreadLocalStoreId { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void SetOverrideDelegate(IntPtr thisHandle, IntPtr pOverride, IntPtr currentMethod);
        public SetOverrideDelegate SetOverride { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void AddActiveDependencyDelegate(IntPtr thisHandle, IntPtr moduleFrom, IntPtr moduleTo);
        public AddActiveDependencyDelegate AddActiveDependency { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetDelegateCtorDelegate(IntPtr thisHandle, IntPtr methHnd, IntPtr clsHnd, IntPtr targetMethodHnd, ref DelegateCtorArgs pCtorData);
        public GetDelegateCtorDelegate GetDelegateCtor { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void MethodCompileCompleteDelegate(IntPtr thisHandle, IntPtr methHnd);
        public MethodCompileCompleteDelegate MethodCompileComplete { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetTailCallCopyArgsThunkDelegate(IntPtr thisHandle, IntPtr pSig, CorInfoHelperTailCallSpecialHandling flags);
        public GetTailCallCopyArgsThunkDelegate GetTailCallCopyArgsThunk { get; }

        [UnmanagedFunctionPointer(default)]
        [return: MarshalAs(UnmanagedType.I1)]
        public delegate bool ConvertPInvokeCalliToCallDelegate(IntPtr thisHandle, ref CORINFO_RESOLVED_TOKEN pResolvedToken, [MarshalAs(UnmanagedType.I1)] bool mustConvert);
        public ConvertPInvokeCalliToCallDelegate ConvertPInvokeCalliToCall { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr GetMemoryManagerDelegate(IntPtr thisHandle, IntPtr ppException);
        public GetMemoryManagerDelegate GetMemoryManager { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void AllocMemDelegate(IntPtr thisHandle, uint hotCodeSize, uint coldCodeSize, uint roDataSize, uint xcptnsCount, CorJitAllocMemFlag flag, ref IntPtr hotCodeBlock, ref IntPtr coldCodeBlock, ref IntPtr roDataBlock);
        public AllocMemDelegate AllocMem { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void ReserveUnwindInfoDelegate(IntPtr thisHandle, [MarshalAs(UnmanagedType.Bool)] bool isFunclet, [MarshalAs(UnmanagedType.Bool)] bool isColdCode, uint unwindSize);
        public ReserveUnwindInfoDelegate ReserveUnwindInfo { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void AllocUnwindInfoDelegate(IntPtr thisHandle, IntPtr pHotCode, IntPtr pColdCode, uint startOffset, uint endOffset, uint unwindSize, IntPtr pUnwindBlock, CorJitFuncKind funcKind);
        public AllocUnwindInfoDelegate AllocUnwindInfo { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate IntPtr AllocGcInfoDelegate(IntPtr thisHandle, UIntPtr size);
        public AllocGcInfoDelegate AllocGcInfo { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void YieldExecutionDelegate(IntPtr thisHandle, IntPtr ppException);
        public YieldExecutionDelegate YieldExecution { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void SetEHcountDelegate(IntPtr thisHandle, uint cEh);
        public SetEHcountDelegate SetEHcount { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void SetEHinfoDelegate(IntPtr thisHandle, uint eHnumber, ref CORINFO_EH_CLAUSE clause);
        public SetEHinfoDelegate SetEHinfo { get; }

        [UnmanagedFunctionPointer(default)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public delegate bool LogMsgDelegate(IntPtr thisHandle, uint level, IntPtr fmt, IntPtr args);
        public LogMsgDelegate LogMsg { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate int DoAssertDelegate(IntPtr thisHandle, IntPtr szFile, int iLine, IntPtr szExpr);
        public DoAssertDelegate DoAssert { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void ReportFatalErrorDelegate(IntPtr thisHandle, CorJitResult result);
        public ReportFatalErrorDelegate ReportFatalError { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate HRESULT AllocMethodBlockCountsDelegate(IntPtr thisHandle, uint count, ref IntPtr pBlockCounts);
        public AllocMethodBlockCountsDelegate AllocMethodBlockCounts { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate HRESULT GetMethodBlockCountsDelegate(IntPtr thisHandle, IntPtr ftnHnd, ref uint pCount, ref IntPtr pBlockCounts, ref uint pNumRuns);
        public GetMethodBlockCountsDelegate GetMethodBlockCounts { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void RecordCallSiteDelegate(IntPtr thisHandle, uint instrOffset, IntPtr callSig, IntPtr methodHandle);
        public RecordCallSiteDelegate RecordCallSite { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void RecordRelocationDelegate(IntPtr thisHandle, IntPtr location, IntPtr target, ushort fRelocType, ushort slotNum, int addlDelta);
        public RecordRelocationDelegate RecordRelocation { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate ushort GetRelocTypeHintDelegate(IntPtr thisHandle, IntPtr target);
        public GetRelocTypeHintDelegate GetRelocTypeHint { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate void GetModuleNativeEntryPointRangeDelegate(IntPtr thisHandle, ref IntPtr pStart, ref IntPtr pEnd);
        public GetModuleNativeEntryPointRangeDelegate GetModuleNativeEntryPointRange { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate uint GetExpectedTargetArchitectureDelegate(IntPtr thisHandle, IntPtr ppException);
        public GetExpectedTargetArchitectureDelegate GetExpectedTargetArchitecture { get; }

        [UnmanagedFunctionPointer(default)]
        public delegate uint GetJitFlagsDelegate(IntPtr thisHandle, ref CORJIT_FLAGS flags, uint sizeInBytes);
        public GetJitFlagsDelegate GetJitFlags { get; }

    }
}
