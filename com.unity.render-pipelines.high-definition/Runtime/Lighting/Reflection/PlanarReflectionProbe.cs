using UnityEngine.Serialization;
using UnityEngine.Rendering;
using UnityEngine.Assertions;
using System;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [ExecuteInEditMode]
    public class PlanarReflectionProbe : HDProbe, ISerializationCallbackReceiver
    {
        [Serializable]
        public struct RenderData
        {
            public Matrix4x4 worldToCameraRHS;
            public Matrix4x4 projectionMatrix;
        }

        const int currentVersion = 2;

        [SerializeField, FormerlySerializedAs("version")]
        int m_Version;

        public enum CapturePositionMode
        {
            Static,
            MirrorCamera,
        }

        [SerializeField]
        Vector3 m_CaptureLocalPosition;
        [SerializeField]
        float m_CaptureNearPlane = 1;
        [SerializeField]
        float m_CaptureFarPlane = 1000;
        [SerializeField]
        CapturePositionMode m_CapturePositionMode = CapturePositionMode.Static;
        [SerializeField]
        Vector3 m_CaptureMirrorPlaneLocalPosition;
        [SerializeField]
        Vector3 m_CaptureMirrorPlaneLocalNormal = Vector3.up;
        [SerializeField]
        bool m_OverrideFieldOfView = false;
        [SerializeField]
        [Range(0, 180)]
        float m_FieldOfViewOverride = 90;

        [SerializeField]
        Vector3 m_LocalReferencePosition = -Vector3.forward;
        [SerializeField]
        RenderData m_BakedRenderData;
        [SerializeField]
        RenderData m_CustomRenderData;
        RenderData m_RealtimeRenderData;

        public override ProbeSettings.ProbeType probeType { get { return ProbeSettings.ProbeType.PlanarProbe; } }

        public RenderData bakedRenderData { get { return m_BakedRenderData; } internal set { m_BakedRenderData = value; } }
        public RenderData customRenderData { get { return m_CustomRenderData; } internal set { m_CustomRenderData = value; } }
        public RenderData realtimeRenderData { get { return m_RealtimeRenderData; } internal set { m_RealtimeRenderData = value; } }
        public RenderData renderData
        {
            get
            {
                switch (mode)
                {
                    default:
                    case ProbeSettings.Mode.Baked:
                        return bakedRenderData;
                    case ProbeSettings.Mode.Custom:
                        return customRenderData;
                    case ProbeSettings.Mode.Realtime:
                        return realtimeRenderData;
                }
            }
        }

        public Vector3 localReferencePosition { get { return m_LocalReferencePosition; } }
        public Vector3 referencePosition { get { return transform.TransformPoint(m_LocalReferencePosition); } }

        public bool overrideFieldOfView { get { return m_OverrideFieldOfView; } }
        public float fieldOfViewOverride { get { return m_FieldOfViewOverride; } }

        public BoundingSphere boundingSphere { get { return influenceVolume.GetBoundingSphereAt(transform); } }

        public Bounds bounds { get { return influenceVolume.GetBoundsAt(transform); } }
        public Vector3 captureLocalPosition { get { return m_CaptureLocalPosition; } set { m_CaptureLocalPosition = value; } }
        public Matrix4x4 influenceToWorld
        {
            get
            {
                var tr = transform;
                var influencePosition = influenceVolume.GetWorldPosition(tr);
                return Matrix4x4.TRS(
                    influencePosition,
                    tr.rotation,
                    Vector3.one
                    );
            }
        }
        public float captureNearPlane { get { return m_CaptureNearPlane; } }
        public float captureFarPlane { get { return m_CaptureFarPlane; } }
        public CapturePositionMode capturePositionMode { get { return m_CapturePositionMode; } }
        public Vector3 captureMirrorPlaneLocalPosition
        {
            get { return m_CaptureMirrorPlaneLocalPosition; }
            set { m_CaptureMirrorPlaneLocalPosition = value; }
        }
        public Vector3 captureMirrorPlanePosition { get { return transform.TransformPoint(m_CaptureMirrorPlaneLocalPosition); } }
        public Vector3 captureMirrorPlaneLocalNormal
        {
            get { return m_CaptureMirrorPlaneLocalNormal; }
            set { m_CaptureMirrorPlaneLocalNormal = value; }
        }
        public Vector3 captureMirrorPlaneNormal { get { return transform.TransformDirection(m_CaptureMirrorPlaneLocalNormal); } }

        protected override void PopulateSettings(ref ProbeSettings settings)
        {
            base.PopulateSettings(ref settings);

            ComputeTransformRelativeToInfluence(
                out settings.proxySettings.mirrorPositionProxySpace,
                out settings.proxySettings.mirrorRotationProxySpace
            );
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            Assert.IsNotNull(influenceVolume, "influenceVolume must have an instance at this point. See HDProbe.Awake()");
            if (m_Version != currentVersion)
            {
                // Add here data migration code
                if(m_Version < 2)
                {
                    influenceVolume.MigrateOffsetSphere();
                }
                m_Version = currentVersion;
            }

            influenceVolume.boxBlendNormalDistanceNegative = Vector3.zero;
            influenceVolume.boxBlendNormalDistancePositive = Vector3.zero;
        }
    }
}
