/* ── Initialization ──────────────────────────────────────────── */
document.addEventListener("DOMContentLoaded", function () {
	console.log("[site.js] DOMContentLoaded fired");
	try { initScrollReveal(); } catch (e) { console.error("initScrollReveal error:", e); }
	try { initQuickView(); } catch (e) { console.error("initQuickView error:", e); }
	try { initDetailGallery(); } catch (e) { console.error("initDetailGallery error:", e); }
	try { initUploadPreview(); } catch (e) { console.error("initUploadPreview error:", e); }
	try { initCartWishlistButtons(); } catch (e) { console.error("initCartWishlistButtons error:", e); }
	try { updateBadges(); } catch (e) { console.error("updateBadges error:", e); }
});

/* ── Scroll Reveal ──────────────────────────────────────────── */

function initScrollReveal() {
	var revealTargets = document.querySelectorAll(".reveal-on-scroll");
	if (revealTargets.length === 0) return;

	var observer = new IntersectionObserver(
		function (entries) {
			entries.forEach(function (entry) {
				if (!entry.isIntersecting) return;
				entry.target.classList.add("is-visible");
				observer.unobserve(entry.target);
			});
		},
		{ threshold: 0.16 }
	);

	revealTargets.forEach(function (target) { observer.observe(target); });
}

/* ── Quick View ─────────────────────────────────────────────── */

function initQuickView() {
	var modal = document.getElementById("quickViewModal");
	var closeButton = document.getElementById("quickViewClose");
	if (!modal || !closeButton) {
		console.log("[site.js] No quickViewModal found, skipping initQuickView");
		return;
	}

	var qvImage = document.getElementById("quickViewImage");
	var qvName = document.getElementById("quickViewName");
	var qvPrice = document.getElementById("quickViewPrice");
	var qvDescription = document.getElementById("quickViewDescription");
	var qvSizeSelect = document.getElementById("quickViewSize");
	var qvAddCartBtn = document.getElementById("quickViewAddCart");
	var qvAddWishlistBtn = document.getElementById("quickViewAddWishlist");

	document.addEventListener("click", function (e) {
		var trigger = e.target.closest(".quick-view-btn");
		if (!trigger) return;

		// Prevent <a> navigation if quick-view-btn is inside a link
		e.preventDefault();
		e.stopPropagation();

		var ds = trigger.dataset;

		if (qvImage) qvImage.src = ds.image || "";
		if (qvName) qvName.textContent = ds.name || "";
		if (qvPrice) qvPrice.textContent = ds.price || "";
		if (qvDescription) qvDescription.textContent = ds.description || "";

		if (qvSizeSelect) {
			qvSizeSelect.innerHTML = "";
			(ds.sizes || "S,M,L").split(",").forEach(function (item) {
				var option = document.createElement("option");
				option.textContent = item.trim();
				option.value = item.trim();
				qvSizeSelect.appendChild(option);
			});
		}

		if (qvAddCartBtn) qvAddCartBtn.dataset.productId = ds.productId || "";
		if (qvAddWishlistBtn) qvAddWishlistBtn.dataset.productId = ds.productId || "";

		modal.classList.add("open");
		modal.setAttribute("aria-hidden", "false");
	});

	closeButton.addEventListener("click", function () { closeQuickView(modal); });
	modal.addEventListener("click", function (event) {
		if (event.target === modal) closeQuickView(modal);
	});

	console.log("[site.js] Quick view initialized");
}

function closeQuickView(modal) {
	modal.classList.remove("open");
	modal.setAttribute("aria-hidden", "true");
}

/* ── Detail Gallery ─────────────────────────────────────────── */

function initDetailGallery() {
	var mainImage = document.getElementById("detailMainImage");
	var thumbs = document.querySelectorAll(".detail-thumb");
	if (!mainImage || thumbs.length === 0) return;

	thumbs.forEach(function (thumb) {
		thumb.addEventListener("click", function () {
			var nextImage = thumb.dataset.image;
			if (nextImage) mainImage.src = nextImage;
		});
	});
}

/* ── Upload Preview ─────────────────────────────────────────── */

function initUploadPreview() {
	var fileInput = document.getElementById("productImages");
	var preview = document.getElementById("imagePreview");
	if (!fileInput || !preview) return;

	fileInput.addEventListener("change", function () {
		preview.innerHTML = "";
		Array.from(fileInput.files || []).forEach(function (file) {
			if (!file.type.startsWith("image/")) return;
			var url = URL.createObjectURL(file);
			var img = document.createElement("img");
			img.src = url;
			img.alt = file.name;
			preview.appendChild(img);
		});
	});
}

/* ── Toast Notifications ────────────────────────────────────── */

function showToast(message, type) {
	type = type || "success";
	var container = document.getElementById("toast-container");
	if (!container) {
		container = document.createElement("div");
		container.id = "toast-container";
		container.className = "toast-container";
		document.body.appendChild(container);
	}

	var toast = document.createElement("div");
	toast.className = "toast toast-" + type;

	var iconMap = {
		success: "fa-check-circle",
		error: "fa-exclamation-circle",
		warning: "fa-exclamation-triangle",
		info: "fa-info-circle"
	};

	toast.innerHTML =
		'<i class="fas ' + (iconMap[type] || iconMap.info) + '"></i>' +
		"<span>" + message + "</span>" +
		'<button class="toast-close" type="button">&times;</button>';

	toast.querySelector(".toast-close").addEventListener("click", function () {
		toast.remove();
	});

	container.appendChild(toast);

	requestAnimationFrame(function () {
		toast.classList.add("toast-show");
	});

	setTimeout(function () {
		toast.classList.remove("toast-show");
		toast.classList.add("toast-hide");
		setTimeout(function () { toast.remove(); }, 350);
	}, 3500);
}

window.showToast = showToast;

/* ── Cart & Wishlist AJAX ───────────────────────────────────── */

function addToCart(productId, qty, size, btnElement) {
	if (!productId) {
		console.warn("[addToCart] No productId");
		return;
	}
	qty = qty || 1;

	console.log("[addToCart] productId=" + productId + " qty=" + qty + " size=" + size);

	if (btnElement) {
		btnElement.disabled = true;
		btnElement.classList.add("btn-loading");
	}

	var url = "/Product/AddToCart?productId=" + encodeURIComponent(productId) + "&qty=" + encodeURIComponent(qty);
	if (size) {
		url += "&size=" + encodeURIComponent(size);
	}

	fetch(url, { method: "POST", credentials: "include" })
		.then(function (resp) {
			console.log("[addToCart] HTTP status:", resp.status, resp.statusText);
			return resp.text().then(function (text) {
				console.log("[addToCart] raw response:", text);
				try {
					return JSON.parse(text);
				} catch (parseErr) {
					console.error("[addToCart] JSON parse error:", parseErr);
					throw new Error("Server trả về dữ liệu không hợp lệ (status " + resp.status + ")");
				}
			});
		})
		.then(function (data) {
			console.log("[addToCart] parsed response:", JSON.stringify(data));

			if (data.requireLogin) {
				showToast(data.message || "Vui lòng đăng nhập", "warning");
				setTimeout(function () { window.location.href = "/Account/Login"; }, 1500);
				return;
			}

			if (data.success) {
				showToast(data.message || "Thêm vào giỏ hàng thành công", "success");
				updateBadges();
			} else {
				console.warn("[addToCart] success=false, data:", data);
				showToast(data.message || "Không thể thêm vào giỏ hàng", "error");
			}
		})
		.catch(function (err) {
			console.error("[addToCart] error:", err);
			showToast(err.message || "Lỗi kết nối server", "error");
		})
		.finally(function () {
			if (btnElement) {
				btnElement.disabled = false;
				btnElement.classList.remove("btn-loading");
			}
		});
}

function addToWishlist(productId, btnElement) {
	if (!productId) {
		console.warn("[addToWishlist] No productId");
		return;
	}

	console.log("[addToWishlist] productId=" + productId);

	if (btnElement) {
		btnElement.disabled = true;
		btnElement.classList.add("btn-loading");
	}

	fetch("/Product/AddToWishlist?productId=" + encodeURIComponent(productId), { method: "POST", credentials: "include" })
		.then(function (resp) { return resp.json(); })
		.then(function (data) {
			console.log("[addToWishlist] response:", data);

			if (data.requireLogin) {
				showToast(data.message || "Vui lòng đăng nhập", "warning");
				setTimeout(function () { window.location.href = "/Account/Login"; }, 1500);
				return;
			}

			if (data.success) {
				if (data.alreadyExisted) {
					showToast("Sản phẩm đã có trong yêu thích", "info");
				} else {
					showToast(data.message || "Đã thêm vào yêu thích", "success");
				}

				if (btnElement) {
					var icon = btnElement.querySelector("i");
					if (icon) {
						icon.classList.remove("far");
						icon.classList.add("fas");
						btnElement.classList.add("wishlisted");
					}
				}

				updateBadges();
			} else {
				showToast(data.message || "Không thể thêm vào yêu thích", "error");
			}
		})
		.catch(function (err) {
			console.error("[addToWishlist] error:", err);
			showToast("Lỗi kết nối server", "error");
		})
		.finally(function () {
			if (btnElement) {
				btnElement.disabled = false;
				btnElement.classList.remove("btn-loading");
			}
		});
}

window.addToCart = addToCart;
window.addToWishlist = addToWishlist;

/* ── Badge Counters ─────────────────────────────────────────── */

function updateBadges() {
	Promise.all([
		fetch("/Product/GetCartCount", { credentials: "include" }).then(function (r) { return r.json(); }),
		fetch("/Product/GetWishlistCount", { credentials: "include" }).then(function (r) { return r.json(); })
	]).then(function (results) {
		var cartData = results[0];
		var wishData = results[1];

		var cartBadge = document.getElementById("cartBadge");
		var wishlistBadge = document.getElementById("wishlistBadge");

		if (cartBadge) {
			if (cartData.count > 0) {
				cartBadge.textContent = cartData.count > 99 ? "99+" : cartData.count;
				cartBadge.classList.add("badge-visible");
			} else {
				cartBadge.textContent = "";
				cartBadge.classList.remove("badge-visible");
			}
		}

		if (wishlistBadge) {
			if (wishData.count > 0) {
				wishlistBadge.textContent = wishData.count > 99 ? "99+" : wishData.count;
				wishlistBadge.classList.add("badge-visible");
			} else {
				wishlistBadge.textContent = "";
				wishlistBadge.classList.remove("badge-visible");
			}
		}
	}).catch(function () {
		// silently ignore badge errors
	});
}

window.updateBadges = updateBadges;

/* ── Size Picker Helper ─────────────────────────────────────── */

function getSelectedSize(cardOrContainer) {
	if (!cardOrContainer) return null;
	var activeChip = cardOrContainer.querySelector(".size-chip.active, .size-chip-mini.active");
	return activeChip ? (activeChip.dataset.size || activeChip.textContent.trim()) : null;
}

/* ── Event Delegation for ALL interactive buttons ───────────── */

function initCartWishlistButtons() {
	document.addEventListener("click", function (e) {

		// ── Size chip selection (both .size-chip and .size-chip-mini) ──
		var sizeChip = e.target.closest(".size-chip, .size-chip-mini");
		if (sizeChip) {
			e.preventDefault();
			e.stopPropagation();
			// Deselect siblings
			var parent = sizeChip.parentElement;
			if (parent) {
				parent.querySelectorAll(".size-chip, .size-chip-mini").forEach(function (c) {
					c.classList.remove("active");
				});
			}
			sizeChip.classList.add("active");
			console.log("[size-chip] Selected: " + (sizeChip.dataset.size || sizeChip.textContent.trim()));
			return;
		}

		// ── Add to cart button (Shop page only, NOT detail-cart-btn) ──
		var cartBtn = e.target.closest(".btn-add-cart");
		if (cartBtn && !cartBtn.classList.contains("detail-cart-btn")) {
			e.preventDefault();
			e.stopPropagation();
			var productId = cartBtn.dataset.productId;
			console.log("[btn-add-cart] Clicked, productId=" + productId);
			if (productId) {
				var card = cartBtn.closest(".shop-card, .wishlist-card, .quick-view-info");
				var size = getSelectedSize(card);

				// Also check quick view dropdown
				if (!size) {
					var qvSize = document.getElementById("quickViewSize");
					var modal = document.getElementById("quickViewModal");
					if (modal && modal.classList.contains("open") && qvSize) {
						size = qvSize.value;
					}
				}

				addToCart(productId, 1, size, cartBtn);
			}
			return;
		}

		// ── Add to wishlist ──
		var wishBtn = e.target.closest(".btn-add-wishlist");
		if (wishBtn) {
			e.preventDefault();
			e.stopPropagation();
			var pid = wishBtn.dataset.productId;
			console.log("[btn-add-wishlist] Clicked, productId=" + pid);
			if (pid) {
				addToWishlist(pid, wishBtn);
			}
			return;
		}
	});

	console.log("[site.js] Cart/Wishlist event delegation registered");
}
