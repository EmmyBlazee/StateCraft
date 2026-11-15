using System;
using UnityEngine;
using UnityEngine.UIElements;
using StateCraft.Data;
using StateCraft.Core;

namespace StateCraft.UI
{
    public class AIDiplomaticProposalData
    {
        public CountryDataSO senderCountry;
        public string proposalType;
        public string title;
        public string message;
        public Action onAccept;
        public Action onDecline;
    }

    public class AIDiplomaticProposalController : MonoBehaviour
    {
        public static AIDiplomaticProposalController Instance { get; private set; }

        private UIDocument _uiDocument;
        private VisualElement _root;
        private VisualElement _modal;
        private Image _flagImg;
        private Label _headerLbl;
        private Label _titleLbl;
        private Label _senderLbl;
        private Label _msgLbl;
        private Button _btnAccept;
        private Button _btnDecline;

        private AIDiplomaticProposalData _currentProposal;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument != null) _root = _uiDocument.rootVisualElement;

            InitializeElements();
        }

        private void Start()
        {
            if (_modal == null) InitializeElements();
        }

        private void InitializeElements()
        {
            if (_root == null && _uiDocument != null) _root = _uiDocument.rootVisualElement;
            if (_root == null)
            {
                foreach (var doc in FindObjectsOfType<UIDocument>())
                {
                    var m = doc.rootVisualElement?.Q<VisualElement>("AIProposalModal");
                    if (m != null)
                    {
                        _root = doc.rootVisualElement;
                        _uiDocument = doc;
                        break;
                    }
                }
            }

            if (_root == null) return;

            _modal      = _root.Q<VisualElement>("AIProposalModal");
            _flagImg    = _modal?.Q<Image>("AIProposalFlag");
            _headerLbl  = _modal?.Q<Label>("AIProposalHeader");
            _titleLbl   = _modal?.Q<Label>("AIProposalTitle");
            _senderLbl  = _modal?.Q<Label>("AIProposalSender");
            _msgLbl     = _modal?.Q<Label>("AIProposalMessage");
            _btnAccept  = _modal?.Q<Button>("BtnAcceptProposal");
            _btnDecline = _modal?.Q<Button>("BtnDeclineProposal");

            if (_btnAccept != null)
            {
                _btnAccept.clicked -= OnAcceptClicked;
                _btnAccept.clicked += OnAcceptClicked;
            }

            if (_btnDecline != null)
            {
                _btnDecline.clicked -= OnDeclineClicked;
                _btnDecline.clicked += OnDeclineClicked;
            }
        }

        public void ShowProposal(AIDiplomaticProposalData proposal)
        {
            if (proposal == null || proposal.senderCountry == null) return;
            _currentProposal = proposal;

            if (_modal == null) InitializeElements();
            if (_modal == null) return;

            if (_titleLbl != null)  _titleLbl.text  = proposal.title.ToUpper();
            if (_senderLbl != null) _senderLbl.text = $"FROM: EMBASSY OF {proposal.senderCountry.countryName.ToUpper()}";
            if (_msgLbl != null)    _msgLbl.text    = proposal.message;

            if (_flagImg != null)
            {
                string flagName = proposal.senderCountry.countryName.Replace(" ", "_");
                Sprite flagSprite = Resources.Load<Sprite>($"Flags/{flagName}");
                _flagImg.sprite = flagSprite != null ? flagSprite : proposal.senderCountry.countryFlag;
            }

            if (_uiDocument != null) _uiDocument.sortingOrder = 200;
            _modal.style.display = DisplayStyle.Flex;
            _modal.pickingMode = PickingMode.Position;
        }

        private void OnAcceptClicked()
        {
            if (_currentProposal != null)
            {
                _currentProposal.onAccept?.Invoke();
            }
            CloseModal();
        }

        private void OnDeclineClicked()
        {
            if (_currentProposal != null)
            {
                _currentProposal.onDecline?.Invoke();
            }
            CloseModal();
        }

        public void CloseModal()
        {
            if (_modal != null)
            {
                _modal.style.display = DisplayStyle.None;
            }
            _currentProposal = null;
        }
    }
}
