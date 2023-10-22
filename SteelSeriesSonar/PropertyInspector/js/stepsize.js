const volumeStepControl = document.getElementById('step');
const tooltip = document.querySelector('.sdpi-info-label'); // This is a bad selector. fix it.

// Why are tooltips like this? ew.
function setToolTipListeners() {
	const fn = () => {
		const tw = tooltip.getBoundingClientRect().width;
		const rangeRect = volumeStepControl.getBoundingClientRect();
		const w = rangeRect.width - tw / 2;
		const percnt =
			(volumeStepControl.value - volumeStepControl.min) /
			(volumeStepControl.max - volumeStepControl.min);
		if (tooltip.classList.contains('hidden')) {
			tooltip.style.top = '-1000px';
		} else {
			tooltip.style.left = `${rangeRect.left + Math.round(w * percnt) - tw / 4}px`;
			const val = Math.round(volumeStepControl.value);
			tooltip.textContent = `+/- ${val} %`;
			tooltip.style.top = `${rangeRect.top - 30}px`;
		}
	};

	volumeStepControl.addEventListener(
		'mouseenter',
		function () {
			tooltip.classList.remove('hidden');
			tooltip.classList.add('shown');
			fn();
		},
		false
	);

	volumeStepControl.addEventListener(
		'mouseout',
		function () {
			tooltip.classList.remove('shown');
			tooltip.classList.add('hidden');
			fn();
		},
		false
	);
	volumeStepControl.addEventListener('input', fn, false);
}
