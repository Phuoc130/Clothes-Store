document.addEventListener("DOMContentLoaded", () => {
	initScrollReveal();
	initQuickView();
	initDetailGallery();
	initUploadPreview();
});

function initScrollReveal() {
	const revealTargets = document.querySelectorAll(".reveal-on-scroll");
	if (revealTargets.length === 0) {
		return;
	}

	const observer = new IntersectionObserver(
		(entries) => {
			entries.forEach((entry) => {
				if (!entry.isIntersecting) {
					return;
				}

				entry.target.classList.add("is-visible");
				observer.unobserve(entry.target);
			});
		},
		{ threshold: 0.16 }
	);

	revealTargets.forEach((target) => observer.observe(target));
}

function initQuickView() {
	const modal = document.getElementById("quickViewModal");
	const closeButton = document.getElementById("quickViewClose");
	const triggers = document.querySelectorAll(".quick-view-btn");

	if (!modal || !closeButton || triggers.length === 0) {
		return;
	}

	const image = document.getElementById("quickViewImage");
	const name = document.getElementById("quickViewName");
	const price = document.getElementById("quickViewPrice");
	const description = document.getElementById("quickViewDescription");
	const sizeSelect = document.getElementById("quickViewSize");

	triggers.forEach((trigger) => {
		trigger.addEventListener("click", () => {
			const { image: imageSource, name: productName, price: productPrice, description: productDescription, sizes } = trigger.dataset;

			if (image) {
				image.src = imageSource || "";
			}
			if (name) {
				name.textContent = productName || "";
			}
			if (price) {
				price.textContent = productPrice || "";
			}
			if (description) {
				description.textContent = productDescription || "";
			}

			if (sizeSelect) {
				sizeSelect.innerHTML = "";
				(sizes || "S,M,L").split(",").forEach((item) => {
					const option = document.createElement("option");
					option.textContent = item.trim();
					option.value = item.trim();
					sizeSelect.appendChild(option);
				});
			}

			modal.classList.add("open");
			modal.setAttribute("aria-hidden", "false");
		});
	});

	closeButton.addEventListener("click", () => closeQuickView(modal));
	modal.addEventListener("click", (event) => {
		if (event.target === modal) {
			closeQuickView(modal);
		}
	});
}

function closeQuickView(modal) {
	modal.classList.remove("open");
	modal.setAttribute("aria-hidden", "true");
}

function initDetailGallery() {
	const mainImage = document.getElementById("detailMainImage");
	const thumbs = document.querySelectorAll(".detail-thumb");

	if (!mainImage || thumbs.length === 0) {
		return;
	}

	thumbs.forEach((thumb) => {
		thumb.addEventListener("click", () => {
			const nextImage = thumb.dataset.image;
			if (nextImage) {
				mainImage.src = nextImage;
			}
		});
	});
}

function initUploadPreview() {
	const fileInput = document.getElementById("productImages");
	const preview = document.getElementById("imagePreview");

	if (!fileInput || !preview) {
		return;
	}

	fileInput.addEventListener("change", () => {
		preview.innerHTML = "";

		Array.from(fileInput.files || []).forEach((file) => {
			if (!file.type.startsWith("image/")) {
				return;
			}

			const url = URL.createObjectURL(file);
			const image = document.createElement("img");
			image.src = url;
			image.alt = file.name;
			preview.appendChild(image);
		});
	});
}

