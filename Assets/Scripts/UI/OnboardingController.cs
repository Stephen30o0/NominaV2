using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Nomina
{
    /// <summary>
    /// Onboarding flow controller — shows 4 step intro screens on first launch.
    /// </summary>
    public class OnboardingController : MonoBehaviour
    {
        [Header("Pages")]
        [SerializeField] private GameObject[] pages;

        [Header("Navigation")]
        [SerializeField] private Button nextButton;
        [SerializeField] private Button skipButton;
        [SerializeField] private Button getStartedButton;
        [SerializeField] private TextMeshProUGUI pageIndicator;

        [Header("Page Dots")]
        [SerializeField] private Image[] pageDots;
        [SerializeField] private Color activeDotColor = Color.white;
        [SerializeField] private Color inactiveDotColor = new Color(1, 1, 1, 0.3f);

        private int currentPage = 0;

        private void Start()
        {
            if (nextButton != null) nextButton.onClick.AddListener(NextPage);
            if (skipButton != null) skipButton.onClick.AddListener(CompleteOnboarding);
            if (getStartedButton != null) getStartedButton.onClick.AddListener(CompleteOnboarding);

            ShowPage(0);
        }

        public void NextPage()
        {
            if (currentPage < pages.Length - 1)
            {
                ShowPage(currentPage + 1);
            }
            else
            {
                CompleteOnboarding();
            }
        }

        public void PreviousPage()
        {
            if (currentPage > 0)
            {
                ShowPage(currentPage - 1);
            }
        }

        private void ShowPage(int index)
        {
            currentPage = index;
            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i] != null)
                    pages[i].SetActive(i == index);
            }

            // Update dots
            if (pageDots != null)
            {
                for (int i = 0; i < pageDots.Length; i++)
                {
                    if (pageDots[i] != null)
                        pageDots[i].color = (i == index) ? activeDotColor : inactiveDotColor;
                }
            }

            // Update page indicator
            if (pageIndicator != null)
                pageIndicator.text = $"{index + 1}/{pages.Length}";

            // Show "Get Started" on last page, "Next" on others
            bool isLastPage = (index == pages.Length - 1);
            if (nextButton != null) nextButton.gameObject.SetActive(!isLastPage);
            if (getStartedButton != null) getStartedButton.gameObject.SetActive(isLastPage);
        }

        private void CompleteOnboarding()
        {
            AppManager.Instance?.CompleteOnboarding();
        }
    }
}