/* ── Initialization ──────────────────────────────────────────── */
document.addEventListener("DOMContentLoaded", function () {
	console.log("[site.js] DOMContentLoaded fired");
	try { initPageTransitions(); } catch (e) { console.error("initPageTransitions error:", e); }
	try { initThemeSwitch(); } catch (e) { console.error("initThemeSwitch error:", e); }
	try { initNavActiveState(); } catch (e) { console.error("initNavActiveState error:", e); }
	try { initHeaderSearchExpand(); } catch (e) { console.error("initHeaderSearchExpand error:", e); }
	try { initScrollReveal(); } catch (e) { console.error("initScrollReveal error:", e); }
	try { initHeroEffects(); } catch (e) { console.error("initHeroEffects error:", e); }
	try { initViewportCardMotion(); } catch (e) { console.error("initViewportCardMotion error:", e); }
	try { initTestimonialsSlider(); } catch (e) { console.error("initTestimonialsSlider error:", e); }
	try { initSmartSearchSuggest(); } catch (e) { console.error("initSmartSearchSuggest error:", e); }
	try { initMiniCartDrawer(); } catch (e) { console.error("initMiniCartDrawer error:", e); }
	try { initQuickView(); } catch (e) { console.error("initQuickView error:", e); }
	try { initDetailGallery(); } catch (e) { console.error("initDetailGallery error:", e); }
	try { initUploadPreview(); } catch (e) { console.error("initUploadPreview error:", e); }
	try { initCartWishlistButtons(); } catch (e) { console.error("initCartWishlistButtons error:", e); }
	try { updateBadges(); } catch (e) { console.error("updateBadges error:", e); }
});

var prevBadgeState = {
	cart: 0,
	wishlist: 0
};

/* ── Page Transition ───────────────────────────────────────── */

function initPageTransitions() {
	var overlay = document.getElementById("pageTransitionOverlay");
	if (!overlay) return;

	overlay.classList.add("is-ready");
	document.body.classList.add("page-enter");
	requestAnimationFrame(function () {
		document.body.classList.add("page-enter-active");
	});

	document.addEventListener("click", function (e) {
		var link = e.target.closest("a");
		if (!link) return;
		if (link.target === "_blank" || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;
		var href = link.getAttribute("href") || "";
		if (!href || href.charAt(0) === "#" || href.indexOf("javascript:") === 0) return;
		if (link.hasAttribute("download")) return;

		var nextUrl;
		try {
			nextUrl = new URL(href, window.location.href);
		} catch (err) {
			return;
		}

		if (nextUrl.origin !== window.location.origin) return;

		e.preventDefault();
		overlay.classList.add("is-leaving");
		window.setTimeout(function () {
			window.location.href = nextUrl.href;
		}, 180);
	});
}

/* ── Header Search Expand ─────────────────────────────────── */

function initHeaderSearchExpand() {
	var searchForm = document.querySelector(".nav-search-form");
	if (!searchForm) return;

	var input = searchForm.querySelector(".nav-search-input");
	if (!input) return;

	input.addEventListener("focus", function () {
		searchForm.classList.add("expanded");
	});

	document.addEventListener("click", function (e) {
		if (e.target.closest(".nav-search-form")) return;
		searchForm.classList.remove("expanded");
	});
}

/* ── Theme Switch ──────────────────────────────────────────── */

function initThemeSwitch() {
	var root = document.documentElement;
	var toggleBtn = document.getElementById("themeToggleBtn");
	var storageKey = "store-theme-v2";
	try { localStorage.removeItem("store-theme"); } catch (e) {}
	var saved = localStorage.getItem(storageKey);
	var theme = saved === "warm" ? "warm" : "luxury";
	root.setAttribute("data-theme", theme);

	if (!toggleBtn) return;

	toggleBtn.addEventListener("click", function () {
		var next = root.getAttribute("data-theme") === "luxury" ? "warm" : "luxury";
		root.setAttribute("data-theme", next);
		localStorage.setItem(storageKey, next);
		if (window.showToast) {
			window.showToast(next === "luxury" ? "Luxury dark theme enabled" : "Warm light theme enabled", "info");
		}
	});
}

/* ── Active Nav State ──────────────────────────────────────── */

function initNavActiveState() {
	var links = document.querySelectorAll(".nav-main-link");
	if (!links.length) return;

	var params = new URLSearchParams(window.location.search || "");
	var category = (params.get("category") || "").toLowerCase();
	var sort = (params.get("sort") || "").toLowerCase();

	links.forEach(function (link) {
		var kind = (link.dataset.navKind || "").toLowerCase();
		var value = (link.dataset.navValue || "").toLowerCase();
		var active = false;

		if (kind === "category" && category === value) {
			active = true;
		}

		if (kind === "sale" && sort === "price-asc") {
			active = true;
		}

		if (active) {
			link.classList.add("is-current");
		}
	});
}

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

/* ── Viewport Card Motion ──────────────────────────────────── */

function initViewportCardMotion() {
	if (window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;

	var cards = document.querySelectorAll(".shop-card, .wishlist-card, .product-item, .category-card");
	if (!cards.length) return;

	var observer = new IntersectionObserver(function (entries) {
		entries.forEach(function (entry) {
			if (!entry.isIntersecting) return;
			entry.target.classList.add("card-inview");
			observer.unobserve(entry.target);
		});
	}, { threshold: 0.18, rootMargin: "0px 0px -20px 0px" });

	cards.forEach(function (card, index) {
		card.classList.add("motion-card");
		card.style.transitionDelay = Math.min(index % 8, 6) * 35 + "ms";
		observer.observe(card);
	});
}

/* ── Testimonials Auto Slider ─────────────────────────────── */

function initTestimonialsSlider() {
	var slider = document.querySelector("[data-auto-slider='testimonials']");
	if (!slider) return;

	var track = slider.querySelector(".testimonials-track");
	var cards = slider.querySelectorAll(".testimonial-card");
	var dotsWrap = slider.querySelector(".testimonials-dots");
	if (!track || !cards.length || !dotsWrap) return;

	var idx = 0;
	var timer = null;

	function renderDots() {
		dotsWrap.innerHTML = "";
		cards.forEach(function (_, i) {
			var dot = document.createElement("button");
			dot.type = "button";
			dot.className = "testimonials-dot" + (i === idx ? " active" : "");
			dot.setAttribute("aria-label", "Go to review " + (i + 1));
			dot.addEventListener("click", function () {
				idx = i;
				update();
				restart();
			});
			dotsWrap.appendChild(dot);
		});
	}

	function update() {
		track.style.transform = "translateX(-" + (idx * 100) + "%)";
		renderDots();
	}

	function start() {
		if (timer) return;
		timer = window.setInterval(function () {
			idx = (idx + 1) % cards.length;
			update();
		}, 4200);
	}

	function stop() {
		if (!timer) return;
		window.clearInterval(timer);
		timer = null;
	}

	function restart() {
		stop();
		start();
	}

	slider.addEventListener("mouseenter", stop);
	slider.addEventListener("mouseleave", start);
	update();
	start();
}

/* ── Hero Effects ───────────────────────────────────────────── */

function initHeroEffects() {
	var hero = document.querySelector(".hero-banner");
	if (!hero) return;
	if (window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches) return;

	var leftOrb = hero.querySelector(".orb-left");
	var rightOrb = hero.querySelector(".orb-right");

	hero.addEventListener("mousemove", function (e) {
		var rect = hero.getBoundingClientRect();
		var x = (e.clientX - rect.left) / rect.width - 0.5;
		var y = (e.clientY - rect.top) / rect.height - 0.5;

		if (leftOrb) {
			leftOrb.style.transform = "translate(" + (x * 18) + "px, " + (y * 14) + "px)";
		}
		if (rightOrb) {
			rightOrb.style.transform = "translate(" + (x * -16) + "px, " + (y * -10) + "px)";
		}
	});

	var scrollHint = document.querySelector(".hero-scroll-hint");
	if (scrollHint) {
		scrollHint.addEventListener("click", function (e) {
			e.preventDefault();
			var target = document.querySelector(".section-space.container");
			if (target) {
				target.scrollIntoView({ behavior: "smooth", block: "start" });
			}
		});
	}
}

/* ── Smart Search Suggest ─────────────────────────────────── */

function initSmartSearchSuggest() {
	var input = document.querySelector(".nav-search-input");
	var panel = document.getElementById("searchSuggest");
	if (!input || !panel) return;

	var timer = null;

	function hidePanel() {
		panel.style.display = "none";
		panel.innerHTML = "";
	}

	function render(items) {
		if (!items.length) {
			panel.innerHTML = '<p class="search-suggest-empty">No matching products found</p>';
			panel.style.display = "block";
			return;
		}

		panel.innerHTML = items.map(function (p) {
			var img = p.imageUrl || "https://images.unsplash.com/photo-1434389677669-e08b4cac3105?q=80&w=300&auto=format&fit=crop";
			var price = (p.discountPercent && p.discountPercent > 0)
				? Math.round(p.price * (1 - p.discountPercent / 100)).toLocaleString("en-US") + " VND"
				: (p.price || 0).toLocaleString("en-US") + " VND";
			return '<a class="search-suggest-item" href="/Product/Details/' + p.product_ID + '">' +
				'<img src="' + img + '" alt="' + p.name + '" loading="lazy" />' +
				'<div><strong>' + p.name + '</strong><div>' + price + '</div></div>' +
				'</a>';
		}).join("");
		panel.style.display = "block";
	}

	input.addEventListener("input", function () {
		var keyword = input.value.trim();
		if (timer) window.clearTimeout(timer);
		if (keyword.length < 2) {
			hidePanel();
			return;
		}

		timer = window.setTimeout(function () {
			fetch("/Product/SearchAjax?keyword=" + encodeURIComponent(keyword) + "&limit=5", {
				headers: { "Accept": "application/json" }
			})
				.then(function (r) { return r.json(); })
				.then(function (data) { render(data.items || []); })
				.catch(function () { hidePanel(); });
		}, 220);
	});

	document.addEventListener("click", function (e) {
		if (e.target.closest(".nav-search-form")) return;
		hidePanel();
	});
}

/* ── Mini Cart Drawer ─────────────────────────────────────── */

function initMiniCartDrawer() {
	var trigger = document.querySelector("[data-mini-cart-trigger='true']");
	var drawer = document.getElementById("miniCartDrawer");
	var overlay = document.getElementById("miniCartOverlay");
	var closeBtn = document.getElementById("miniCartClose");
	var body = document.getElementById("miniCartBody");
	if (!trigger || !drawer || !overlay || !closeBtn || !body) return;

	function closeDrawer() {
		drawer.classList.remove("open");
		drawer.setAttribute("aria-hidden", "true");
		overlay.style.display = "none";
	}

	function openDrawer() {
		drawer.classList.add("open");
		drawer.setAttribute("aria-hidden", "false");
		overlay.style.display = "block";
		loadMiniCart();
	}

	function loadMiniCart() {
		body.innerHTML = '<p class="mini-cart-empty">Loading cart...</p>';
		fetch("/Product/GetCartItems")
			.then(function (r) { return r.json(); })
			.then(function (data) {
				if (data.requireLogin) {
					body.innerHTML = '<p class="mini-cart-empty">Please sign in to view your cart</p>';
					return;
				}
				var items = Array.isArray(data) ? data : (data.items || []);
				if (!items.length) {
					body.innerHTML = '<p class="mini-cart-empty">Your cart is empty</p>';
					return;
				}
				body.innerHTML = items.map(function (item) {
					var img = item.imageUrl || "https://images.unsplash.com/photo-1434389677669-e08b4cac3105?q=80&w=300&auto=format&fit=crop";
					var sub = (item.price * item.quantity).toLocaleString("en-US") + " VND";
					return '<article class="mini-cart-item">' +
						'<img src="' + img + '" alt="' + item.productName + '" />' +
						'<div class="mini-cart-meta"><p><strong>' + item.productName + '</strong></p>' +
						'<p>SL: ' + item.quantity + ' · Size: ' + (item.selectedSize || "-") + '</p>' +
						'<p>' + sub + '</p></div>' +
						'</article>';
				}).join("");
			})
			.catch(function () {
				body.innerHTML = '<p class="mini-cart-empty">Unable to load cart</p>';
			});
	}

	trigger.addEventListener("click", function (e) {
		e.preventDefault();
		openDrawer();
	});

	closeBtn.addEventListener("click", closeDrawer);
	overlay.addEventListener("click", closeDrawer);
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
	var qvPrev = document.getElementById("quickViewPrev");
	var qvNext = document.getElementById("quickViewNext");
	var qvName = document.getElementById("quickViewName");
	var qvPrice = document.getElementById("quickViewPrice");
	var qvDescription = document.getElementById("quickViewDescription");
	var qvSizeSelect = document.getElementById("quickViewSize");
	var qvAddCartBtn = document.getElementById("quickViewAddCart");
	var qvAddWishlistBtn = document.getElementById("quickViewAddWishlist");
	var gallery = [];
	var galleryIndex = 0;

	function renderGalleryImage() {
		if (!qvImage || !gallery.length) return;
		qvImage.src = gallery[galleryIndex];
	}

	function moveGallery(step) {
		if (!gallery.length) return;
		galleryIndex = (galleryIndex + step + gallery.length) % gallery.length;
		renderGalleryImage();
	}

	document.addEventListener("click", function (e) {
		var trigger = e.target.closest(".quick-view-btn");
		if (!trigger) return;

		// Prevent <a> navigation if quick-view-btn is inside a link
		e.preventDefault();
		e.stopPropagation();

		var ds = trigger.dataset;
		gallery = [];
		if (ds.images) {
			ds.images.split("|").forEach(function (item) {
				var normalized = (item || "").trim();
				if (normalized && gallery.indexOf(normalized) === -1) {
					gallery.push(normalized);
				}
			});
		}
		if (ds.image && gallery.indexOf(ds.image) === -1) {
			gallery.unshift(ds.image);
		}
		if (!gallery.length && ds.image) {
			gallery = [ds.image];
		}
		galleryIndex = 0;

		renderGalleryImage();
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

	if (qvPrev) {
		qvPrev.addEventListener("click", function () {
			moveGallery(-1);
		});
	}

	if (qvNext) {
		qvNext.addEventListener("click", function () {
			moveGallery(1);
		});
	}

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
					throw new Error("Server returned invalid data (status " + resp.status + ")");
				}
			});
		})
		.then(function (data) {
			console.log("[addToCart] parsed response:", JSON.stringify(data));

			if (data.requireLogin) {
				showToast(data.message || "Please sign in", "warning");
				setTimeout(function () { window.location.href = "/Account/Login"; }, 1500);
				return;
			}

			if (data.success) {
				showToast(data.message || "Added to cart successfully", "success");
				updateBadges();
			} else {
				console.warn("[addToCart] success=false, data:", data);
				showToast(data.message || "Unable to add to cart", "error");
			}
		})
		.catch(function (err) {
			console.error("[addToCart] error:", err);
			showToast(err.message || "Server connection error", "error");
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
				showToast(data.message || "Please sign in", "warning");
				setTimeout(function () { window.location.href = "/Account/Login"; }, 1500);
				return;
			}

			if (data.success) {
				if (data.alreadyExisted) {
					showToast("Product already in wishlist", "info");
				} else {
					showToast(data.message || "Added to wishlist", "success");
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
				showToast(data.message || "Unable to add to wishlist", "error");
			}
		})
		.catch(function (err) {
			console.error("[addToWishlist] error:", err);
			showToast("Server connection error", "error");
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
			if (cartData.count > prevBadgeState.cart) {
				cartBadge.classList.remove("bump");
				requestAnimationFrame(function () { cartBadge.classList.add("bump"); });
			}
				cartBadge.textContent = cartData.count > 99 ? "99+" : cartData.count;
				cartBadge.classList.add("badge-visible");
			} else {
				cartBadge.textContent = "";
				cartBadge.classList.remove("badge-visible");
				cartBadge.classList.remove("bump");
			}
			prevBadgeState.cart = cartData.count || 0;
		}

		if (wishlistBadge) {
			if (wishData.count > 0) {
			if (wishData.count > prevBadgeState.wishlist) {
				wishlistBadge.classList.remove("bump");
				requestAnimationFrame(function () { wishlistBadge.classList.add("bump"); });
			}
				wishlistBadge.textContent = wishData.count > 99 ? "99+" : wishData.count;
				wishlistBadge.classList.add("badge-visible");
			} else {
				wishlistBadge.textContent = "";
				wishlistBadge.classList.remove("badge-visible");
				wishlistBadge.classList.remove("bump");
			}
			prevBadgeState.wishlist = wishData.count || 0;
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
